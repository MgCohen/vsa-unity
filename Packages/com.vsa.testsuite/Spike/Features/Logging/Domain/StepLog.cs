namespace VSA.TestSuite.Spike
{
    /// <summary>Numbers the steps of one run. One instance for each app, so a new run starts at 1.</summary>
    public sealed class StepLog
    {
        int count;

        public int Count => count;

        public int Next() => ++count;
    }
}
