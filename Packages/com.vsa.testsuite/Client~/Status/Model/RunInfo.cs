namespace VSA.TestSuite.Client.Status
{
    /// <summary>One run folder in the log root: one Editor start, or one batch-mode compile check.</summary>
    public sealed class RunInfo
    {
        public string Id;
        /// <summary>"editor" or "precheck".</summary>
        public string Kind;
        public DateTime? StartedUtc;
        public int? Pid;
        /// <summary>True if the PID of the run is still a running Unity Editor.</summary>
        public bool Running;
        public string Folder;
        public string UnityLog;
        public long UnityLogBytes;
        /// <summary>The watch recording. Null if no watch ran.</summary>
        public string WatchLog;
    }
}
