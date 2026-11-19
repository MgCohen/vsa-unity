using System;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using VSA.TestSuite.Spike.Features.Recording;

namespace VSA.TestSuite.Spike.Editor
{
    public sealed class UnityVideoRecorder : IVideoRecorder
    {
        RecorderController controller;
        RecorderControllerSettings settings;
        MovieRecorderSettings movie;
        double deadline;

        public string Name => "Unity Recorder";

        public string Unavailable => Application.isPlaying ? null : "The Unity Recorder records only in Play Mode";

        public bool IsRecording => controller != null && controller.IsRecording();

        public event Action<string> Stopped;

        public string Start(string pathNoExtension, RecordingSettings options)
        {
            Stop();
            Prepare(pathNoExtension, options);
            controller = new RecorderController(settings);
            try
            {
                controller.PrepareRecording();
                if (!controller.StartRecording())
                    throw new InvalidOperationException("The Unity Recorder did not start. The console has the reason");
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
            return pathNoExtension.Replace('\\', '/') + ".mp4";
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

        void Prepare(string pathNoExtension, RecordingSettings options)
        {
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
            movie.ImageInputSettings = EvenGameView();
            movie.OutputFile = pathNoExtension.Replace('\\', '/');
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Variable;
            settings.FrameRate = options.FrameRate;
        }

        static GameViewInputSettings EvenGameView()
        {
            var input = new GameViewInputSettings();
            var width = Screen.width & ~1;
            var height = Screen.height & ~1;
            if (width == Screen.width && height == Screen.height) return input;
            input.OutputWidth = width;
            input.OutputHeight = height;
            return input;
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

        void SelfStop(string reason)
        {
            if (controller == null) return;
            Debug.LogWarning("[Spike] recording stops by itself: " + reason);
            Stop();
            Stopped?.Invoke(reason);
        }

        void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) SelfStop("Play Mode exit");
        }

        void OnReload() => SelfStop("domain reload");

        void OnQuit() => SelfStop("quit");

        void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup > deadline) SelfStop("time limit");
        }
    }
}
