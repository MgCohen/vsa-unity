using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Probe.Editor {
    // Batch mode entries:
    //   -executeMethod Probe.Editor.ProbeEditor.Run      runs the checks in the Editor (Mono)
    //   -executeMethod Probe.Editor.ProbeEditor.Build    builds the Windows IL2CPP player
    //       -probeOutput <folder>  -probeStripping Minimal|Low|Medium|High (default Minimal)
    //       -probeCodeGeneration OptimizeSpeed|OptimizeSize (default OptimizeSpeed)
    //       -probeBackend IL2CPP|Mono2x (default IL2CPP)
    public static class ProbeEditor {
        const string Scene = "Assets/Probe.unity";

        public static void Run() {
            var failed = ProbeRunner.RunAll();
            ProbeRunner.Line("done", failed == 0 ? "ok" : "FAIL " + failed + " failed");
            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        public static void Build() {
            var output = Argument("-probeOutput") ?? "Builds/Player";
            var stripping = (ManagedStrippingLevel)Enum.Parse(
                typeof(ManagedStrippingLevel), Argument("-probeStripping") ?? "Minimal");
            var target = NamedBuildTarget.Standalone;
            var backend = (ScriptingImplementation)Enum.Parse(
                typeof(ScriptingImplementation), Argument("-probeBackend") ?? "IL2CPP");
            PlayerSettings.SetScriptingBackend(target, backend);
            PlayerSettings.SetManagedStrippingLevel(target, stripping);
            var generation = (Il2CppCodeGeneration)Enum.Parse(
                typeof(Il2CppCodeGeneration), Argument("-probeCodeGeneration") ?? "OptimizeSpeed");
            PlayerSettings.SetIl2CppCodeGeneration(target, generation);
            SaveScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene },
                locationPathName = System.IO.Path.Combine(output, "Probe.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("CHECK build " + (ok ? "ok" : "FAIL " + report.summary.result) + " " + backend + ", stripping " + stripping
                + ", code generation " + generation + ", " + report.summary.totalTime.TotalSeconds.ToString("0") + " s");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        // Opens the probe scene and enters Play Mode. ShellProbe leaves Play Mode at the end, then the Editor exits.
        // The domain reloads when Play Mode starts, so SessionState keeps the request across the reload.
        public static void Play() {
            SaveScene();
            SessionState.SetBool(PlayKey, true);
            EditorApplication.playModeStateChanged += ExitAfterPlay;
            EditorApplication.EnterPlaymode();
        }

        const string PlayKey = "Probe.Play";

        [InitializeOnLoadMethod]
        static void WatchPlay() {
            if (SessionState.GetBool(PlayKey, false)) {
                EditorApplication.playModeStateChanged += ExitAfterPlay;
            }
        }

        static void ExitAfterPlay(PlayModeStateChange change) {
            if (change == PlayModeStateChange.EnteredEditMode) {
                SessionState.EraseBool(PlayKey);
                EditorApplication.Exit(0);
            }
        }

        // The scene of the player and of Play Mode: the shell, and the probe that checks it.
        [MenuItem("Probe/Write Probe Scene")]
        public static void SaveScene() {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var shell = new GameObject("Shell").AddComponent<ProbeShell>();
            var settings = new SerializedObject(shell).FindProperty("settings");
            settings.arraySize = 1;
            settings.GetArrayElementAtIndex(0).objectReferenceValue = Lab.LabSettingsAsset.Write();
            settings.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("ShellProbe").AddComponent<ShellProbe>();
            EditorSceneManager.SaveScene(scene, Scene);
        }

        static string Argument(string name) {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
