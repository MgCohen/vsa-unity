namespace StatusLab
{
    /// <summary>Fallback of the probe group: the main thread answers and no flag is set.</summary>
    public sealed class ReadyIdle : IEditorState
    {
        public string Name => "ready-idle";

        public StateAction Action => StateAction.None;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeOk;
    }
}
