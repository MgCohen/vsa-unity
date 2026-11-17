namespace StatusLab
{
    /// <summary>The answer of the status tracker for one sample.</summary>
    public sealed class StateResult
    {
        public string State;
        /// <summary>Null, "stalled", "stuck" or "loop".</summary>
        public string Result;
        public StateAction Action;
        public long AgeMs;
        public long SinceProgressMs;
        public string Evidence;

        public override string ToString() => State + (Result == null ? "" : "/" + Result);
    }
}
