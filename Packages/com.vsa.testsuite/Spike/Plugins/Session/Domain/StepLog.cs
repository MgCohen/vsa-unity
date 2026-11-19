namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class StepLog
    {
        int count;

        public int Count => count;

        public int Next() => ++count;
    }
}
