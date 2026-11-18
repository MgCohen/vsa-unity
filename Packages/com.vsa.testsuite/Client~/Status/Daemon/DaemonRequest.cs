namespace VSA.TestSuite.Client.Status
{
    /// <summary>One request from a CLI client to the daemon. One JSON line on the pipe.</summary>
    public sealed class DaemonRequest
    {
        /// <summary>"open", "status", "wait" or "close".</summary>
        public string Command;
        /// <summary>wait and open: the states to wait for.</summary>
        public List<string> Until;
        /// <summary>wait and open: the time limit.</summary>
        public long TimeoutMs;
        /// <summary>close: "save", "discard" or null. Null refuses to close with unsaved changes.</summary>
        public string Changes;
    }
}
