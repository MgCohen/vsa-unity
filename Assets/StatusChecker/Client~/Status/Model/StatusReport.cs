namespace VSA.StatusChecker
{
    /// <summary>The answer of "status": the state now, from the running watch or from one sample.</summary>
    public sealed class StatusReport
    {
        public string State;
        /// <summary>Null, "stalled", "stuck" or "loop".</summary>
        public string Result;
        public StateAction Action;
        public long AgeMs;
        public long SinceProgressMs;
        /// <summary>True if the state comes from a running watch, which has the history of the Editor.
        /// False for one sample: then "cold-start" can also be a domain reload, and no clock has run.</summary>
        public bool History;
        public int Pid;
        public string EditorId;
        public string RunFolder;
        public Snapshot Signals;
    }
}
