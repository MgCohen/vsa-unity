namespace VSA.StatusChecker
{
    /// <summary>What the playbook knows: inside [T0, T1] the results must follow Allowed in order.
    /// An entry "a|b" allows either state. Entries can be skipped.</summary>
    public sealed class TruthWindow
    {
        public string Case;
        public string Step;
        public long T0;
        public long T1;
        public List<string> Allowed = new();
        public long DetectMs;
        /// <summary>The state that must show within DetectMs. Null: the first change counts.</summary>
        public string Key;
        /// <summary>A result that must show in the window, for example "stalled". Null: none.</summary>
        public string ExpectResult;
        /// <summary>False for a cleanup window: recorded, not scored.</summary>
        public bool Scored = true;
    }
}
