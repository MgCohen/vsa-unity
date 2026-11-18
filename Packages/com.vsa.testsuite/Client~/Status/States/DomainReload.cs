namespace VSA.TestSuite.Client.Status
{
    /// <summary>No server after a ready, or a reload event.</summary>
    public sealed class DomainReload : IEditorState
    {
        public string Name => "domain-reload";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ServerUp(memory) && (memory.EverReady || now.BeaconLastEvent == "reload-start");
    }
}
