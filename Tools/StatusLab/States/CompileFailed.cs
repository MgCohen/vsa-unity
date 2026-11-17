namespace StatusLab
{
    /// <summary>The last compile failed.</summary>
    public sealed class CompileFailed : IEditorState
    {
        public string Name => "compile-failed";

        public StateAction Action => StateAction.Fail;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeOk && now.TruthFailed == true;
    }
}
