namespace VSA.TestSuite.Client.Status
{
    /// <summary>When a wait stops before its state is met. Pure functions: no Editor, no clock.</summary>
    public static class WaitRules
    {
        /// <summary>The states of "ready": the main thread answers and nothing waits.</summary>
        public static readonly IReadOnlyList<string> Ready = new[] { "ready-idle", "play-running", "play-paused" };

        static readonly HashSet<string> NoEditorStates = new() { "no-editor", "stale-descriptor", "exited" };

        /// <summary>Parses "ready" or a comma list of state names. Throws ArgumentException for an unknown name.</summary>
        public static HashSet<string> ParseUntil(string text)
        {
            var set = new HashSet<string>();
            foreach (var part in (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part == "ready") set.UnionWith(Ready);
                else if (States.All.Any(s => s.Name == part)) set.Add(part);
                else throw new ArgumentException("Unknown state '" + part + "'. Use 'ready' or names of: " + string.Join(", ", States.All.Select(s => s.Name)));
            }
            if (set.Count == 0) throw new ArgumentException("Give at least one state.");
            return set;
        }

        /// <summary>The error code that stops a wait, or null to wait more. A wait has no supervisor:
        /// nothing answers a dialog, restarts a crash or ends a stall.</summary>
        public static string StopCode(StateResult result, bool editorRequired = true)
        {
            switch (result.Result)
            {
                case "loop": return "Loop";
                case "stalled": return "Stalled";
                case "stuck": return "Stuck";
            }
            if (result.Action == StateAction.Answer) return "Dialog";
            if (result.Action == StateAction.Fail) return Pascal(result.State);
            if (result.State == "crash") return "Crash";
            if (editorRequired && NoEditorStates.Contains(result.State)) return "NoEditor";
            return null;
        }

        /// <summary>"compile-failed" to "CompileFailed".</summary>
        public static string Pascal(string name)
            => string.Concat(name.Split('-').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p.Substring(1)));
    }
}
