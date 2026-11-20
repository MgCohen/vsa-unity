using System;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public interface IVideoRecorder
    {
        string Name { get; }

        string Unavailable { get; }

        bool IsRecording { get; }

        event Action<string> Stopped;

        string Start(string pathNoExtension, RecordingSettings settings);

        void Stop();
    }
}
