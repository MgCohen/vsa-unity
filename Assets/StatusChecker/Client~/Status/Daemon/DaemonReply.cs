namespace VSA.StatusChecker
{
    /// <summary>The answer of the daemon to one request. One JSON line on the pipe.</summary>
    public sealed class DaemonReply
    {
        /// <summary>Null on success. Else the error code, for example "Stalled", "SafeMode" or "UnsavedChanges".</summary>
        public string Code;
        public string Message;
        /// <summary>"starting", "running", "closing", "ended" or "failed".</summary>
        public string Phase;
        /// <summary>"adopt" (the daemon can stop and restart the Editor) or "observe" (it only answers dialogs).</summary>
        public string Mode;
        /// <summary>True if the daemon found the Editor running and did not start it.</summary>
        public bool Attached;
        public int DaemonPid;
        public StatusReport Report;
        /// <summary>Warning codes, for example "not-automated" or "no-beacon".</summary>
        public List<string> Warnings = new();
        /// <summary>close: the paths of the scenes with unsaved changes.</summary>
        public List<string> DirtyScenes;
        /// <summary>The compile or package errors of a refused start.</summary>
        public List<string> Errors;
        /// <summary>close: the exit code of the Editor process.</summary>
        public int? ExitCode;
    }
}
