using System;

namespace VSA.TestSuite.Features.Recording
{
    public sealed class VideoFile
    {
        public string Name { get; set; }

        public string File { get; set; }

        public DateTime StartedAt { get; set; }

        public float Seconds { get; set; }

        public bool Active { get; set; }

        public string StopReason { get; set; }

        public long Bytes { get; set; }
    }
}
