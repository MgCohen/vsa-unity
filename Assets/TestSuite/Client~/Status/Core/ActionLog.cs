namespace VSA.TestSuite.Client.Status
{
    /// <summary>What the supervisor and the playbook did. The tracker reads it; the recorder writes each entry as one line.</summary>
    public sealed class ActionLog
    {
        readonly List<ActionEntry> entries = new();
        readonly IClock clock;

        public ActionLog(IClock clock) => this.clock = clock;

        public event Action<ActionEntry> Added;

        public IReadOnlyList<ActionEntry> Entries => entries;

        public void Add(string who, string kind, string detail = null)
            => Add(new ActionEntry { T = clock.NowMs, Who = who, Kind = kind, Detail = detail });

        /// <summary>Adds a recorded entry as it is. Replay uses this.</summary>
        public void Add(ActionEntry entry)
        {
            entries.Add(entry);
            Added?.Invoke(entry);
        }
    }
}
