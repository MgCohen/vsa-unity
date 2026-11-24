namespace VSA.TestSuite.Client.Status
{
    /// <summary>No own Editor, and the descriptor names a dead PID.</summary>
    public sealed class StaleDescriptor : IEditorState
    {
        public string Name => "stale-descriptor";

        public StateAction Action => StateAction.Start;

        public bool Matches(Snapshot now, Memory memory)
            => memory.OwnPid == 0 && now.DescPresent && !now.DescPidAlive;
    }
}
