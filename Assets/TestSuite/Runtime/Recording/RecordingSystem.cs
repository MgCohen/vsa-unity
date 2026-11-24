using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>The settings of a video recording. Change them in the composition root.</summary>
    public sealed class RecordingOptions
    {
        /// <summary>The video width in pixels. Null: the size of the Game view, and the recorder does not change it.</summary>
        public int? Width;
        /// <summary>The video height in pixels. Null: the size of the Game view.</summary>
        public int? Height;
        /// <summary>The highest frame rate of the video. The game time does not change.</summary>
        public float FrameRate = 30;
        public bool Audio;
        /// <summary>A recording that is longer than this stops by itself. It is the last safety net if no stop call comes.</summary>
        public int MaxSeconds = 600;
        /// <summary>If true, each <c>suite.run</c> step records a video into its step folder.</summary>
        public bool RecordScripts = true;

        public RecordingOptions Copy() => (RecordingOptions)MemberwiseClone();
    }

    /// <summary>Why a recording stopped.</summary>
    public enum StopReason
    {
        /// <summary>The step or the command that started it stopped it.</summary>
        Stopped,
        /// <summary>The step that started it failed.</summary>
        StepFailed,
        /// <summary>The recording was longer than <see cref="RecordingOptions.MaxSeconds"/>.</summary>
        MaxTime,
        /// <summary>Play Mode stopped.</summary>
        PlayModeExit,
        /// <summary>A domain reload started.</summary>
        DomainReload,
        /// <summary>The application quit.</summary>
        Quit,
        /// <summary>A new recording started while this one ran.</summary>
        Replaced,
    }

    /// <summary>One recording: the file, the times, and why it stopped.</summary>
    public sealed class Recording
    {
        public string Name;
        public string File;
        public string Owner;
        public DateTime StartedAt;
        public double Seconds;
        /// <summary>True while the recording runs.</summary>
        public bool Active;
        [JsonConverter(typeof(StringEnumConverter))]
        public StopReason? Reason;
        /// <summary>The file size in bytes after the stop. 0: the recorder wrote no file.</summary>
        public long Bytes;
    }

    /// <summary>
    /// The adapter to a video recorder. The package gives a Unity Recorder adapter for the Editor.
    /// Each adapter must stop by itself on Play Mode exit, domain reload, quit and <c>MaxSeconds</c>,
    /// and then raise <see cref="Stopped"/>.
    /// </summary>
    public interface IVideoRecorder
    {
        string Name { get; }
        /// <summary>Null if the recorder can record now. Else the reason it cannot.</summary>
        string Unavailable { get; }
        bool IsRecording { get; }
        /// <summary>Starts a recording. <paramref name="pathNoExtension"/> has no extension. Returns the full file path.</summary>
        string Start(string pathNoExtension, RecordingOptions options);
        /// <summary>Stops the recording. It does nothing if no recording runs.</summary>
        void Stop();
        /// <summary>The adapter stopped by itself, for example at Play Mode exit.</summary>
        event Action<StopReason> Stopped;
    }

    /// <summary>The recorder when no adapter is installed.</summary>
    public sealed class NoVideoRecorder : IVideoRecorder
    {
        readonly string reason;
        public NoVideoRecorder(string reason) => this.reason = reason;
        public string Name => "none";
        public string Unavailable => reason;
        public bool IsRecording => false;
        public string Start(string pathNoExtension, RecordingOptions options) => throw new ActionFailed(FailCode.NoDevice, reason);
        public void Stop() { }
        public event Action<StopReason> Stopped { add { } remove { } }
    }

    /// <summary>The default recorder adapter. An Editor assembly sets it when the Unity Recorder package is installed.</summary>
    public static class VideoRecorders
    {
        /// <summary>Makes the default adapter. The Editor assembly of the package sets it at load.</summary>
        public static Func<IVideoRecorder> Default = () => new NoVideoRecorder(Application.isEditor
            ? "The Unity Recorder package (com.unity.recorder) is not installed"
            : "Video recording works only in the Editor");
    }

    /// <summary>
    /// Starts and stops one video recording at a time. Stop is safe to call at all times:
    /// with no recording, it does nothing.
    /// </summary>
    public sealed class RecordingSystem
    {
        readonly IVideoRecorder recorder;
        System.Diagnostics.Stopwatch watch;

        public RecordingOptions Options { get; }
        /// <summary>The recording that runs now, or null.</summary>
        public Recording Current { get; private set; }
        /// <summary>The last recording that stopped, or null.</summary>
        public Recording Last { get; private set; }
        public string RecorderName => recorder.Name;
        public string Unavailable => recorder.Unavailable;

        public RecordingSystem(IVideoRecorder recorder, RecordingOptions options = null)
        {
            this.recorder = recorder;
            Options = options ?? new RecordingOptions();
            recorder.Stopped += reason => Finish(reason);
        }

        /// <summary>Starts a recording into <paramref name="folder"/>. If a recording runs, it stops first.</summary>
        /// <param name="maxSeconds">The time limit of this recording. Null: <see cref="RecordingOptions.MaxSeconds"/>.</param>
        public Recording Start(string folder, string name, string owner, int? maxSeconds = null)
        {
            var why = recorder.Unavailable;
            if (why != null) throw new ActionFailed(FailCode.NoDevice, why);
            if (Current != null) Stop(StopReason.Replaced);

            Directory.CreateDirectory(folder);
            var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var options = Options;
            if (maxSeconds.HasValue)
            {
                options = Options.Copy();
                options.MaxSeconds = maxSeconds.Value;
            }
            var file = recorder.Start(Path.Combine(folder, safe), options);
            watch = System.Diagnostics.Stopwatch.StartNew();
            Current = new Recording { Name = name, File = file, Owner = owner, StartedAt = DateTime.Now, Active = true };
            Debug.Log("[TestSuite] Recording started: " + file);
            return Current;
        }

        /// <summary>Stops the recording that runs now. Returns it, or the last recording if none runs.</summary>
        public Recording Stop(StopReason reason = StopReason.Stopped)
        {
            if (Current == null) return Last;
            try
            {
                recorder.Stop();
            }
            catch (Exception e)
            {
                // The state must clear even if the recorder fails, or the next start is blocked.
                Debug.LogException(e);
            }
            return Finish(reason);
        }

        Recording Finish(StopReason reason)
        {
            var r = Current;
            if (r == null) return Last;
            Current = null;
            r.Active = false;
            r.Reason = reason;
            r.Seconds = Math.Round(watch?.Elapsed.TotalSeconds ?? 0, 2);
            r.Bytes = System.IO.File.Exists(r.File) ? new FileInfo(r.File).Length : 0;
            Last = r;
            Debug.Log("[TestSuite] Recording stopped (" + reason + ", " + r.Seconds + " s): " + r.File);
            return r;
        }
    }
}
