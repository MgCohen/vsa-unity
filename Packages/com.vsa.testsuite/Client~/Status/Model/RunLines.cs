namespace VSA.TestSuite.Client.Status
{
    /// <summary>The last lines of the logs of one run.</summary>
    public sealed class RunLines
    {
        public RunInfo Run;
        public List<string> UnityLog = new();
        public List<string> StatusLog = new();
    }
}
