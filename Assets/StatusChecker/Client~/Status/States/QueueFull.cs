namespace VSA.StatusChecker
{
    /// <summary>The command queue is full: the probe gets HTTP 429.</summary>
    public sealed class QueueFull : IEditorState
    {
        public string Name => "queue-full";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeHttp == 429;
    }
}
