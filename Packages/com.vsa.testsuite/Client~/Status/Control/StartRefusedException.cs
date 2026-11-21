namespace VSA.TestSuite.Client.Status
{
    /// <summary>The control did not start the Editor: the batch-mode compile check failed,
    /// or an Editor already runs on the project.</summary>
    public sealed class StartRefusedException : Exception
    {
        public StartRefusedException(string message, IReadOnlyList<string> errors, string logPath)
            : base(message)
        {
            Errors = errors;
            LogPath = logPath;
        }

        /// <summary>The compile or package error lines of the check log.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>The log of the batch-mode compile check. Null if the check did not run.</summary>
        public string LogPath { get; }
    }
}
