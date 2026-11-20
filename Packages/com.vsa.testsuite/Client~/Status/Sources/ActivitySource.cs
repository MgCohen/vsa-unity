namespace VSA.TestSuite.Client.Status
{
    /// <summary>activity: size of TestRuns/activity.log in the project. The test suite adds one line for each
    /// request start and end, and one line each second of a long wait, so the file grows while an input works.</summary>
    public sealed class ActivitySource : ISignalSource
    {
        public string Key => "activity";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (target.ProjectPath == null) return Task.CompletedTask;
            var path = Path.Combine(target.ProjectPath, "TestRuns", "activity.log");
            try
            {
                if (File.Exists(path)) into.ActivitySize = new FileInfo(path).Length;
            }
            catch (IOException) { }
            return Task.CompletedTask;
        }
    }
}
