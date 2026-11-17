namespace VSA.TestSuite.Spike
{
    /// <summary>Result of session.end: true if an app ran and stopped.</summary>
    public sealed record SessionEnded(bool Stopped);
}
