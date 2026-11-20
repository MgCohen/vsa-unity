using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed class RecordingSystem
    {
        readonly IVideoRecorder recorder;
        readonly RecordingSettings settings;
        Stopwatch watch;

        public RecordingSystem(IVideoRecorder recorder, RecordingSettings settings)
        {
            this.recorder = recorder;
            this.settings = settings;
            recorder.Stopped += reason => Finish(reason);
        }

        public VideoFile Current { get; private set; }

        public VideoFile Last { get; private set; }

        public string RecorderName => recorder.Name;

        public string Unavailable => recorder.Unavailable;

        public VideoFile Start(string folder, string name)
        {
            var why = recorder.Unavailable;
            if (why != null) throw new InvalidOperationException(why);
            if (Current != null) Stop("replaced");
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var file = recorder.Start(Path.Combine(folder, safe), settings);
            watch = Stopwatch.StartNew();
            Current = new VideoFile { Name = name, File = file, StartedAt = DateTime.Now, Active = true };
            Debug.Log("[Spike] recording started: " + file);
            return Current;
        }

        public VideoFile Stop(string reason)
        {
            if (Current == null) return Last;
            try
            {
                recorder.Stop();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return Finish(reason);
        }

        VideoFile Finish(string reason)
        {
            var video = Current;
            if (video == null) return Last;
            Current = null;
            video.Active = false;
            video.StopReason = reason;
            video.Seconds = (float)(watch?.Elapsed.TotalSeconds ?? 0);
            video.Bytes = File.Exists(video.File) ? new FileInfo(video.File).Length : 0;
            Last = video;
            Debug.Log($"[Spike] recording stopped ({reason}, {video.Seconds:0.0} s): {video.File}");
            return video;
        }
    }
}
