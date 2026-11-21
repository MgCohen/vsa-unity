using System;
using UnityEngine;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed class NoVideoRecorder : IVideoRecorder
    {
        public string Name => "none";

        public string Unavailable => Application.isEditor
            ? "No video recorder: the Unity Recorder package (com.unity.recorder) is not installed"
            : "No video recorder: video recording works only in the Editor";

        public bool IsRecording => false;

        public event Action<string> Stopped
        {
            add { }
            remove { }
        }

        public string Start(string pathNoExtension, RecordingSettings settings)
            => throw new InvalidOperationException(Unavailable);

        public void Stop()
        {
        }
    }
}
