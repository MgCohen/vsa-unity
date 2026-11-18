namespace VSA.TestSuite.Cli
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
        };

        /// <summary>Error code to hint: what the caller does next.</summary>
        static readonly Dictionary<string, string> Hints = new()
        {
            ["NoEditor"] = "No own Editor runs. Run 'vsa-unity watch' to start one, or pass --pid.",
            ["ForeignEditor"] = "An Editor that this tool did not start has the project. Pass --pid to read it. Never stop it.",
            ["Timeout"] = "Read data.state. Wait again with a longer --timeout, or run 'vsa-unity watch'.",
            ["Dialog"] = "A native dialog blocks the Editor. Run 'vsa-unity watch' to answer it by policy.",
            ["Stalled"] = "Nothing moved for the stall window. Run 'vsa-unity watch' to escalate.",
            ["Stuck"] = "The state passed its cap. Run 'vsa-unity watch' to escalate.",
            ["Loop"] = "The same step or dialog repeats. Stop the run and read the log.",
            ["Crash"] = "The Editor crashed. Read 'vsa-unity log show'. 'vsa-unity watch' starts it again one time.",
            ["CompileFailed"] = "Fix the compile errors, then wait again.",
            ["SafeMode"] = "Fix the compile errors, close the Editor by PID, and start again.",
            ["StartFailed"] = "Read the errors in 'vsa-unity log show'. Do not start again before a fix.",
            ["StartRefused"] = "The batch-mode compile check failed. Fix the errors in its log first.",
            ["NotAutomated"] = "The Editor runs without -automated. Start it with 'vsa-unity watch'.",
            ["SupervisorFailed"] = "The supervisor gave up. Read the actions and 'vsa-unity log show'.",
            ["Cancelled"] = "The caller stopped the command.",
            ["BadArguments"] = "Run 'vsa-unity help'.",
            ["NotAProject"] = "Run from a Unity project folder, or pass --project-path.",
            ["NotFound"] = "Run 'vsa-unity log sessions' for the run ids.",
        };

        public static int For(string code) => code == null ? Success : Map.TryGetValue(code, out var exit) ? exit : Failed;

        public static string Hint(string code) => code != null && Hints.TryGetValue(code, out var hint) ? hint : null;

        public static bool Retryable(string code) => code is "Timeout" or "Cancelled";
    }
}
