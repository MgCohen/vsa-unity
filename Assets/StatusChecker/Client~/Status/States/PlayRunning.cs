namespace VSA.StatusChecker
{
    /// <summary>Play Mode, frames run.</summary>
    public sealed class PlayRunning : IEditorState
    {
        public string Name => "play-running";

        public StateAction Action => StateAction.None;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeOk && now.ProbePlaying == true;
    }
}
