using System;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class TestSession
    {
        public string Id { get; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        public DateTime StartedUtc { get; } = DateTime.UtcNow;

        public string Folder { get; set; }

        public string EndRequest { get; set; }
    }
}
