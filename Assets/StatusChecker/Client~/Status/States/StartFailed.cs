namespace VSA.StatusChecker
{
    /// <summary>The own Editor exited with a non-zero code before any ready, with no crash: for example a package that cannot resolve. A restart gives the same result.</summary>
    public sealed class StartFailed : IEditorState
    {
        public string Name => "start-failed";

        public StateAction Action => StateAction.Fail;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ProcAlive && !now.LogCrashSeen && !memory.EverReady && !memory.StopSent && now.ProcExitCode is int code && code != 0;
    }
}
