namespace VSA.TestSuite.Client.Status
{
    /// <summary>The probe gets no answer, but work moves: progress, an own job, or the beacon. Unity's busy window does not count: it also shows during a freeze.</summary>
    public sealed class LongCommand : IEditorState
    {
        public string Name => "long-command";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProgressActive == true || memory.JobActive || now.BeaconFresh;
    }
}
