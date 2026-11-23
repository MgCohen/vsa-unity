using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StatusLab
{
    // Stops the Pipeline server for some seconds while the main thread keeps running, then starts it again.
    // PipelineServerStartup is internal to Unity.Pipeline.Editor, so this uses reflection.
    static class ServerSwitch
    {
        static double restartAt;

        public static void StopFor(int seconds)
        {
            var type = Type.GetType("Unity.Pipeline.Editor.PipelineServerStartup, Unity.Pipeline.Editor");
            if (type == null) { Debug.LogError("STATUSLAB_SERVER_OFF no PipelineServerStartup"); return; }
            Debug.Log("STATUSLAB_SERVER_OFF " + seconds);
            type.GetMethod("StopServer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
            restartAt = EditorApplication.timeSinceStartup + seconds;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < restartAt) return;
            EditorApplication.update -= Tick;
            var type = Type.GetType("Unity.Pipeline.Editor.PipelineServerStartup, Unity.Pipeline.Editor");
            type?.GetMethod("EnsureServerStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
            Debug.Log("STATUSLAB_SERVER_ON");
        }
    }
}
