namespace VSA.StatusChecker
{
    /// <summary>Milliseconds since the start of the recording. Wall time live, recorded time in replay.</summary>
    public interface IClock
    {
        long NowMs { get; }
    }
}
