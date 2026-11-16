namespace VSA.TestSuite.Spike
{
    /// <summary>What one run did: the number of actions, sub-actions included, and how many failed.</summary>
    public sealed record RunSummary(int Actions, int Failed);
}
