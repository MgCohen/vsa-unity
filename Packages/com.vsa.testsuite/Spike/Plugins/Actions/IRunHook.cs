namespace VSA.TestSuite.Spike
{
    /// <summary>A hook that does work when the run ends, for example an export or a recording stop.</summary>
    internal interface IRunHook
    {
        void RunEnded(RunSummary summary);
    }
}
