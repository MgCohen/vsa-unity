using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite.Editor
{
    /// <summary>
    /// Writes Library/VSA.TestSuite/Status/beacon.json from the main thread: once each second from
    /// EditorApplication.update, and at once for each event. A reader in another process sees a still file
    /// when the main thread is blocked. It runs only in an Editor started with the -vsaStatusBeacon flag.
    /// </summary>
    [InitializeOnLoad]
    public static class StatusBeacon
    {
        /// <summary>The command-line flag that starts the beacon.</summary>
        public const string Flag = "-vsaStatusBeacon";

        const string EventsKey = "VSA.TestSuite.BeaconEvents";
        const int MaxEvents = 12;

        static readonly int Pid = Process.GetCurrentProcess().Id;
        static long seq;
        static double lastWrite;

        static StatusBeacon()
        {
            if (!Enabled) return;
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

        /// <summary>True if the command line has the -vsaStatusBeacon flag.</summary>
        public static bool Enabled { get; } = Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0;

        /// <summary>Library/VSA.TestSuite/Status of this project. Not Temp/: Unity can clear Temp/ at a start.</summary>
        public static string Folder
        {
            get
            {
                var path = Path.GetFullPath("Library/VSA.TestSuite/Status");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>Adds an event to the beacon and writes the file at once.</summary>
        public static void Event(string name)
        {
            if (!Enabled) return;
            // SessionState keeps the event list across domain reloads.
            var entry = "{\"name\":\"" + name + "\",\"utc\":\"" + Now() + "\"}";
            var lines = (SessionState.GetString(EventsKey, "") + "\n" + entry)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var start = Math.Max(0, lines.Length - MaxEvents);
            SessionState.SetString(EventsKey, string.Join("\n", lines, start, lines.Length - start));
            Write();
        }

        static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastWrite < 1.0) return;
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
            var path = Path.Combine(Folder, "beacon.json");
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
