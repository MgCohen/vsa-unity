namespace VSA.TestSuite.Client.Status
{
    /// <summary>The own PID is gone after a clean quit: exit code 0, a quit event, or exit lines in the log.</summary>
    public sealed class Exited : IEditorState
    {
        public string Name => "exited";

        public StateAction Action => StateAction.None;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ProcAlive && !now.LogCrashSeen && (now.ProcExitCode == 0 || now.BeaconLastEvent == "quit" || now.LogExitSeen);
    }
}
