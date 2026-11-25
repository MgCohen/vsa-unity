namespace VSA.StatusChecker
{
    /// <summary>Fallback of all states: the server answers, the main thread does not, and nothing moves.</summary>
    public sealed class Frozen : IEditorState
    {
        public string Name => "frozen";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => true;
    }
}
