namespace StatusLab
{
    /// <summary>No own Editor, but a live Editor owns the descriptor of this project.</summary>
    public sealed class ForeignEditor : IEditorState
    {
        public string Name => "foreign-editor";

        public StateAction Action => StateAction.Fail;

        public bool Matches(Snapshot now, Memory memory)
            => memory.OwnPid == 0 && now.DescPresent && now.DescPidAlive;
    }
}
