namespace VSA.StatusChecker.Cli
{
    /// <summary>The exit codes of the unity CLI, and the map from each error code to one of them.</summary>
    static class ExitCodes
    {
        public const int Success = 0;
        public const int Failed = 6;
        public const int NoTarget = 12;
        public const int BadArguments = 13;
        public const int Timeout = 14;
        public const int Busy = 17;
        public const int AwaitingInput = 18;

        /// <summary>Error code to exit code. A code that is not in the map exits with 6.</summary>
        static readonly Dictionary<string, int> Map = new()
        {
            ["NoEditor"] = NoTarget,
            ["BadArguments"] = BadArguments,
            ["NotAProject"] = BadArguments,
            ["NotFound"] = BadArguments,
            ["Timeout"] = Timeout,
            ["Dialog"] = AwaitingInput,
            ["Busy"] = Busy,
        };

        /// <summary>Warning code to text.</summary>
        static readonly Dictionary<string, string> Warnings = new()
        {
            ["not-automated"] = "The Editor runs without -automated: start dialogs can block it. Start it with 'vsa-unity open'.",
            ["no-beacon"] = "The Editor has no status beacon (the package is missing): a freeze shows only through the probe.",
        };

        /// <summary>Error code to hint: what the caller does next.</summary>
        static readonly Dictionary<string, string> Hints = new()
        {
            ["NoEditor"] = "No Editor runs. Run 'vsa-unity open'.",
            ["ForeignEditor"] = "An Editor that no daemon tracks has the project. Run 'vsa-unity open' to attach to it.",
            ["Timeout"] = "Read data.state. Wait again with a longer --timeout.",
            ["Dialog"] = "A native dialog blocks the Editor. Run 'vsa-unity open': its daemon answers dialogs by policy.",
            ["Stalled"] = "Nothing moved for the stall window. In adopt mode the daemon escalates; else run 'vsa-unity close --discard'.",
            ["Stuck"] = "The state passed its cap. In adopt mode the daemon escalates; else run 'vsa-unity close --discard'.",
            ["Loop"] = "The same step or dialog repeats. Stop the run and read the log.",
            ["Crash"] = "The Editor crashed. Read 'vsa-unity log show'. In adopt mode the daemon starts it again one time.",
            ["CompileFailed"] = "Fix the compile errors, then wait again.",
            ["SafeMode"] = "Fix the compile errors, close the Editor by PID, and start again.",
            ["StartFailed"] = "Read the errors in 'vsa-unity log show'. Do not start again before a fix.",
            ["StartRefused"] = "The batch-mode compile check failed. Fix the errors in its log first.",
            ["NotAutomated"] = "The Editor runs without -automated. Close it and run 'vsa-unity open'.",
            ["UnsavedChanges"] = "Read data.dirtyScenes. Run 'vsa-unity close --save' or 'vsa-unity close --discard'.",
            ["UnknownChanges"] = "The main thread does not answer. Run 'vsa-unity close --save' or 'vsa-unity close --discard'.",
            ["CloseFailed"] = "The Editor still runs after quit, window close and stop. Read 'vsa-unity log show'.",
            ["AttachFailed"] = "Another user or a protected process owns the Editor. Close it yourself.",
            ["Busy"] = "A close runs. Wait, then call again.",
            ["SupervisorFailed"] = "The supervisor gave up. Read the actions and 'vsa-unity log show'.",
            ["Cancelled"] = "The caller stopped the command.",
            ["BadArguments"] = "Run 'vsa-unity help'.",
            ["NotAProject"] = "Run from a Unity project folder, or pass --project-path.",
            ["NotFound"] = "Run 'vsa-unity log sessions' for the run ids.",
        };

        public static int For(string code) => code == null ? Success : Map.TryGetValue(code, out var exit) ? exit : Failed;

        public static string Warning(string code) => Warnings.TryGetValue(code, out var text) ? text : code;

        public static string Hint(string code) => code != null && Hints.TryGetValue(code, out var hint) ? hint : null;

        public static bool Retryable(string code) => code is "Timeout" or "Cancelled";
    }
}
