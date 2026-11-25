namespace VSA.TestSuite.Features.Recording
{
    public sealed class RecordingSettings
    {
        public bool RecordSessions { get; set; }

        public float FrameRate { get; set; } = 30;

        public bool Audio { get; set; }

        public int MaxSeconds { get; set; } = 600;
    }
}
