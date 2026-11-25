namespace VSA.StatusChecker
{
    /// <summary>Fallback of the server group: the own Editor runs, but its server does not answer yet.</summary>
    public sealed class ColdStart : IEditorState
    {
        public string Name => "cold-start";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => !now.ServerUp(memory);
    }
}
