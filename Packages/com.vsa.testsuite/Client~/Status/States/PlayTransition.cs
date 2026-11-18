namespace VSA.TestSuite.Client.Status
{
    /// <summary>No server for less than 30 s after a Play Mode command.</summary>
    public sealed class PlayTransition : IEditorState
    {
        public string Name => "play-transition";

        public StateAction Action => StateAction.Retry;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ServerUp(memory) && memory.EverReady && memory.LastPlayCommandMs is long at && now.T - at < 30_000;
    }
}
