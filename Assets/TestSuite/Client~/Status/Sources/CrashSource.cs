namespace VSA.TestSuite.Client.Status
{
    /// <summary>crash: a new folder in the Unity crash folder after the own Editor started.</summary>
    public sealed class CrashSource : ISignalSource
    {
        static readonly string Folder = Path.Combine(Path.GetTempPath(), "Unity", "Editor", "Crashes");

        public string Key => "crash";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (target.StartedUtc == default || !Directory.Exists(Folder)) return Task.CompletedTask;
            into.CrashNew = Directory.EnumerateDirectories(Folder)
                .Any(d => Directory.GetCreationTimeUtc(d) > target.StartedUtc);
            return Task.CompletedTask;
        }
    }
}
