namespace VSA.TestSuite.Client.Status
{
    /// <summary>One thing that the supervisor or the playbook did.</summary>
    public sealed class ActionEntry
    {
        public long T;
        public string Who;                // "playbook" or "supervisor"
        public string Kind;               // "play-command", "job-start", "job-end", "answer", "escalate", "start", "step-result", ...
        public string Detail;
    }
}
