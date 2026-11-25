namespace VSA.StatusChecker
{
    /// <summary>The own PID is gone with no sign of a clean quit, or the log has "Crash!!!" (the process lives about 30 s more).</summary>
    public sealed class Crash : IEditorState
    {
        public string Name => "crash";

        public StateAction Action => StateAction.Start;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ProcAlive || now.LogCrashSeen;
    }
}
