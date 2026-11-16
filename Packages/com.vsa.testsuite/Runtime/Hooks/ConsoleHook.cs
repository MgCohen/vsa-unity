using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>
    /// Keeps the Unity log lines of each step. Writes console.json to the step folder
    /// when the step fails, or when the step logged a warning or an error.
    /// </summary>
    public sealed class ConsoleHook : IActionHook
    {
        readonly object gate = new object();
        List<Entry> entries;
        bool listening;

        sealed class Entry
        {
            public string Level;
            public string Message;
            public string Stack;
        }

        public Task Before(ActionContext ctx)
        {
            if (ctx.Depth != 0) return Task.CompletedTask;
            lock (gate) entries = new List<Entry>();
            if (!listening)
            {
                Application.logMessageReceivedThreaded += OnLog;
                listening = true;
            }
            return Task.CompletedTask;
        }

        public Task After(ActionContext ctx)
        {
            if (ctx.Depth != 0) return Task.CompletedTask;
            List<Entry> taken;
            lock (gate) { taken = entries; entries = null; }
            if (taken == null || taken.Count == 0) return Task.CompletedTask;

            bool problem = taken.Exists(e => e.Level != nameof(LogType.Log));
            if (ctx.Outcome.Failed || problem)
                File.WriteAllText(ctx.Step.File("console.json"), JsonConvert.SerializeObject(taken, Formatting.Indented));
            return Task.CompletedTask;
        }

        void OnLog(string message, string stack, LogType type)
        {
            lock (gate)
            {
                if (entries == null) return;
                entries.Add(new Entry
                {
                    Level = type.ToString(),
                    Message = message,
                    Stack = type == LogType.Log ? null : stack,
                });
            }
        }
    }
}
