using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.SceneManagement;

namespace StatusLab
{
    // Writes Library/StatusLab/beacon.json from the main thread: once each second from
    // EditorApplication.update, and at once for each event. A reader on another thread
    // sees a still file when the main thread is blocked.
    [InitializeOnLoad]
    static class StatusBeacon
    {
        const string EventsKey = "StatusLab.BeaconEvents";
        const int MaxEvents = 12;

        static readonly int Pid = Process.GetCurrentProcess().Id;
        static long seq;
        static double lastWrite;

        static StatusBeacon()
        {
            if (!StatusLabGate.Active) return;
            EditorApplication.update += OnUpdate;
            CompilationPipeline.compilationStarted += _ => Event("compile-start");
            CompilationPipeline.compilationFinished += _ => Event("compile-end");
            AssemblyReloadEvents.beforeAssemblyReload += () => Event("reload-start");
            AssemblyReloadEvents.afterAssemblyReload += () => Event("reload-end");
            EditorApplication.playModeStateChanged += change => Event("play-" + change);
            EditorApplication.pauseStateChanged += change => Event("pause-" + change);
            EditorApplication.quitting += () => Event("quit");
            Event("load");
        }

        public static void Event(string name)
        {
            if (!StatusLabGate.Active) return;
            // One event on each line. SessionState keeps the list across domain reloads.
            var entry = "{\"name\":\"" + name + "\",\"utc\":\"" + Now() + "\"}";
            var lines = (SessionState.GetString(EventsKey, "") + "\n" + entry)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var start = Math.Max(0, lines.Length - MaxEvents);
            SessionState.SetString(EventsKey, string.Join("\n", lines, start, lines.Length - start));
            Write();
        }

        static void OnUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - lastWrite < 1.0) return;
            Write();
        }

        static void Write()
        {
            lastWrite = EditorApplication.timeSinceStartup;
            seq++;
            var dirty = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) dirty++;
            var json = new StringBuilder()
                .Append("{\"pid\":").Append(Pid)
                .Append(",\"seq\":").Append(seq)
                .Append(",\"utc\":\"").Append(Now()).Append('"')
                .Append(",\"compiling\":").Append(Bool(EditorApplication.isCompiling))
                .Append(",\"updating\":").Append(Bool(EditorApplication.isUpdating))
                .Append(",\"playing\":").Append(Bool(EditorApplication.isPlaying))
                .Append(",\"paused\":").Append(Bool(EditorApplication.isPaused))
                .Append(",\"dirtyScenes\":").Append(dirty)
                .Append(",\"events\":[").Append(SessionState.GetString(EventsKey, "").Replace('\n', ',')).Append("]}")
                .ToString();
            var path = Path.Combine(StatusLabGate.Folder, "beacon.json");
            var temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, json);
                File.Copy(temp, path, true);
            }
            catch (IOException)
            {
                // A reader holds the file. The next write tries again.
            }
        }

        static string Bool(bool value) => value ? "true" : "false";

        static string Now() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }
}
