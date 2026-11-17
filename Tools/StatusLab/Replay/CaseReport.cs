namespace StatusLab
{
    /// <summary>The V3 result of one truth window.</summary>
    public sealed class CaseReport
    {
        public string File;
        public string Case;
        public string Step;
        public bool Scored;
        public bool Pass;
        public string Why;
        public List<string> Allowed;
        public List<string> Observed;
        public long? FirstChangeMs;
        public long DetectMs;
        public List<string> Results;
        public string ExpectResult;
        public long DurationMs;
    }
}
