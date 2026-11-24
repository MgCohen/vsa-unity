using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VSA.TestSuite
{
    /// <summary>After each top action, writes summary.json to the run folder: one row for each step.</summary>
    public sealed class SummaryHook : IActionHook
    {
        sealed class Row
        {
            public string Step;
            public string Action;
            public string Script;
            public bool Ok;
            public double Ms;
            public string Code;
            public string Reason;
        }

        readonly Dictionary<RunFolder, List<Row>> rows = new Dictionary<RunFolder, List<Row>>();

        public Task Before(ActionContext ctx) => Task.CompletedTask;

        public Task After(ActionContext ctx)
        {
            if (ctx.Depth != 0 || ctx.Step.Run == null) return Task.CompletedTask;
            if (!rows.TryGetValue(ctx.Step.Run, out var list)) rows[ctx.Step.Run] = list = new List<Row>();

            var error = ctx.Outcome.Error;
            list.Add(new Row
            {
                Step = ctx.Step.Id,
                Action = ctx.Name,
                Script = ctx.Step.Script,
                Ok = !ctx.Outcome.Failed,
                Ms = System.Math.Round(ctx.Outcome.Ms, 1),
                Code = error == null ? null : error is ActionFailed f ? f.Code.ToString() : error.GetType().Name,
                Reason = error?.Message,
            });

            int failed = list.FindAll(r => !r.Ok).Count;
            var summary = new { steps = list.Count, passed = list.Count - failed, failed, rows = list };
            File.WriteAllText(Path.Combine(ctx.Step.Run.Path, "summary.json"), JsonConvert.SerializeObject(summary, Formatting.Indented));
            return Task.CompletedTask;
        }
    }
}
