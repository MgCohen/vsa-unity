namespace VSA.TestSuite.Client.Status
{
    /// <summary>No own Editor and no live descriptor.</summary>
    public sealed class NoEditor : IEditorState
    {
        public string Name => "no-editor";

        public StateAction Action => StateAction.Start;

        public bool Matches(Snapshot now, Memory memory)
            => memory.OwnPid == 0;
    }
}
