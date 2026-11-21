namespace VSA.TestSuite.Client.Status
{
    /// <summary>The own Editor runs, but the descriptor names another live Editor. The own Editor writes it again at its next heartbeat (15 s).</summary>
    public sealed class DescriptorMismatch : IEditorState
    {
        public string Name => "descriptor-mismatch";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => memory.OwnPid != 0 && now.ProcAlive && now.DescPresent && now.DescPidAlive && now.DescPid != memory.OwnPid;
    }
}
