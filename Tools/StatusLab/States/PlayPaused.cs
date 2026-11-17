namespace StatusLab
{
    /// <summary>Play Mode, paused.</summary>
    public sealed class PlayPaused : IEditorState
    {
        public string Name => "play-paused";

        public StateAction Action => StateAction.None;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeOk && now.ProbePlaying == true && now.ProbePaused == true;
    }
}
