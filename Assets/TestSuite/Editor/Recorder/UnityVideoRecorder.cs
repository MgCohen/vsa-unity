using System;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace VSA.TestSuite.Recorder
{
    /// <summary>Makes the Unity Recorder adapter the default recorder when the Editor loads.</summary>
    [InitializeOnLoad]
    static class UnityVideoRecorderInstall
    {
        static UnityVideoRecorderInstall() => VideoRecorders.Default = () => new UnityVideoRecorder();
    }

    /// <summary>
    /// Records the Game view to an MP4 file with the Unity Recorder. It stops by itself at
    /// Play Mode exit, domain reload, Editor quit and the time limit, so no recording stays open.
    /// </summary>
    public sealed class UnityVideoRecorder : IVideoRecorder
    {
        RecorderController controller;
        RecorderControllerSettings settings;
        MovieRecorderSettings movie;
        double deadline;

        public string Name => "Unity Recorder";
        public event Action<StopReason> Stopped;

        public string Unavailable => Application.isPlaying ? null : "The Unity Recorder records only in Play Mode";

        public bool IsRecording => controller != null && controller.IsRecording();

        public string Start(string pathNoExtension, RecordingOptions options)
        {
            Stop();
            settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "VSA TestSuite";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium,
            };
            movie.CaptureAudio = options.Audio;

            // With no size, the input uses the Game view size and the Recorder does not resize the Game view.
            var input = new GameViewInputSettings();
            if (options.Width.HasValue && options.Height.HasValue)
            {
                input.OutputWidth = options.Width.Value;
                input.OutputHeight = options.Height.Value;
            }
            movie.ImageInputSettings = input;
            movie.OutputFile = pathNoExtension.Replace('\\', '/');

            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            // Constant mode sets Time.captureFramerate and changes the game timing. Variable mode does not.
            settings.FrameRatePlayback = FrameRatePlayback.Variable;
            settings.FrameRate = options.FrameRate;

            controller = new RecorderController(settings);
            try
            {
                controller.PrepareRecording();
                if (!controller.StartRecording())
                    throw new ActionFailed(FailCode.Failed, "The Unity Recorder did not start. The console has the reason");
            }
            catch
            {
                Release();
                throw;
            }

            deadline = EditorApplication.timeSinceStartup + options.MaxSeconds;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += OnReload;
            EditorApplication.quitting += OnQuit;
            EditorApplication.update += OnUpdate;
            return pathNoExtension + ".mp4";
        }

        public void Stop()
        {
            if (controller == null) return;
            try
            {
                controller.StopRecording();
            }
            finally
            {
                Release();
            }
        }

        void Release()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= OnReload;
            EditorApplication.quitting -= OnQuit;
            EditorApplication.update -= OnUpdate;
            controller = null;
            if (movie != null) UnityEngine.Object.DestroyImmediate(movie);
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
            movie = null;
            settings = null;
        }

        void SelfStop(StopReason reason)
        {
            if (controller == null) return;
            Debug.LogWarning("[TestSuite] Recording stops by itself: " + reason);
            Stop();
            Stopped?.Invoke(reason);
        }

        // ExitingPlayMode comes before the Recorder removes its scene objects, so the file closes correctly.
        void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) SelfStop(StopReason.PlayModeExit);
        }

        void OnReload() => SelfStop(StopReason.DomainReload);
        void OnQuit() => SelfStop(StopReason.Quit);

        void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup > deadline) SelfStop(StopReason.MaxTime);
        }
    }
}
