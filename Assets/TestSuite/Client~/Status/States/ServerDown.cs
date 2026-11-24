namespace VSA.TestSuite.Client.Status
{
    /// <summary>No server, but the main thread runs (the beacon moves) and Unity had no event (load, compile, reload, play) in the last 10 s, or 30 s at a start.</summary>
    public sealed class ServerDown : IEditorState
    {
        public string Name => "server-down";

        public StateAction Action => StateAction.Escalate;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ServerUp(memory) && now.BeaconFresh && now.BeaconLastEventAgeS > (memory.EverReady ? 10 : 30);
    }
}
