namespace VSA.StatusChecker
{
    /// <summary>The end of a wait: the state was met, or the wait stopped with an error code.</summary>
    public sealed class WaitOutcome
    {
        /// <summary>True if the state is one of the states that the caller waits for.</summary>
        public bool Met;
        /// <summary>Null if met. Else the error code, for example "Timeout", "Stalled", "Dialog" or "CompileFailed".</summary>
        public string Code;
        public string Message;
        public StateResult Result;
        public Snapshot Snapshot;
        public long ElapsedMs;
    }
}
