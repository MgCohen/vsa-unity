using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>Writes one log line before and after each action, indented by depth.</summary>
    public sealed class LogHook : IActionHook
    {
        public Task Before(ActionContext ctx)
        {
            Debug.Log("[TestSuite] " + new string(' ', ctx.Depth * 2) + "> " + ctx.Name + " " + JsonConvert.SerializeObject(ctx.Action));
            return Task.CompletedTask;
        }

        public Task After(ActionContext ctx)
        {
            var o = ctx.Outcome;
            var line = "[TestSuite] " + new string(' ', ctx.Depth * 2) + "< " + ctx.Name + " " + o.Ms.ToString("0.0") + " ms "
                + (o.Failed ? "FAILED " + o.Error.Message : "ok");
            if (o.Failed && ctx.Depth == 0) Debug.LogWarning(line); else Debug.Log(line);
            return Task.CompletedTask;
        }
    }

    /// <summary>After the top action of a step, writes step.json with the full action tree.</summary>
    public sealed class StepRecordHook : IActionHook
    {
        public Task Before(ActionContext ctx) => Task.CompletedTask;

        public Task After(ActionContext ctx)
        {
            if (ctx.Depth != 0) return Task.CompletedTask;
            var json = JsonConvert.SerializeObject(new { step = ctx.Step.Id, frame = Time.frameCount, tree = Node(ctx) }, Formatting.Indented);
            File.WriteAllText(ctx.Step.File("step.json"), json);
            return Task.CompletedTask;
        }

        static object Node(ActionContext c) => new
        {
            action = c.Name,
            args = c.Action,
            ms = System.Math.Round(c.Outcome?.Ms ?? 0, 1),
            ok = c.Outcome != null && !c.Outcome.Failed,
            error = c.Outcome?.Error?.Message,
            result = c.Outcome?.Result,
            children = c.Children.Select(Node).ToList(),
        };
    }

    /// <summary>After a failed top action in Play Mode, saves a screenshot to the step folder.</summary>
    public sealed class ScreenshotOnFailHook : IActionHook
    {
        public Task Before(ActionContext ctx) => Task.CompletedTask;

        public async Task After(ActionContext ctx)
        {
            if (ctx.Depth != 0 || !ctx.Outcome.Failed || !Application.isPlaying) return;
            var path = ctx.Step.File("fail.png");
            ScreenCapture.CaptureScreenshot(path);
            // The file is written at the end of the frame.
            await Frames.Wait(2, System.Threading.CancellationToken.None, 2000);
        }
    }
}
