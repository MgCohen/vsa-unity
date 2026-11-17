using System;

namespace VSA.TestSuite.Spike
{
    /// <summary>One test session: one instance for each app run. A new session gets a new id.</summary>
    public sealed class TestSession
    {
        public string Id { get; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public DateTime StartedUtc { get; } = DateTime.UtcNow;
    }
}
