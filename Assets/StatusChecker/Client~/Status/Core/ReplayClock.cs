namespace VSA.StatusChecker
{
    /// <summary>Replay time: the recorded time of the current sample.</summary>
    public sealed class ReplayClock : IClock
    {
        public long NowMs { get; set; }
    }
}
