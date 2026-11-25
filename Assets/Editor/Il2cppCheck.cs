using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Il2cppCheck {
    const string Scene = "Assets/Check.unity";

    public static void Build() {
        var output = Argument("-checkOutput") ?? Path.GetFullPath("Il2cppCheck/Player");
        var useBackend = Enum.Parse<ScriptingImplementation>(Argument("-checkBackend") ?? "IL2CPP");
        var useStripping = Enum.Parse<ManagedStrippingLevel>(Argument("-checkStripping") ?? "Minimal");
        MakeSceneWithShellAndCheck();
        var target = NamedBuildTarget.Standalone;
        var backend = PlayerSettings.GetScriptingBackend(target);
        var stripping = PlayerSettings.GetManagedStrippingLevel(target);
        BuildReport report;
        try {
            PlayerSettings.SetScriptingBackend(target, useBackend);
            PlayerSettings.SetManagedStrippingLevel(target, useStripping);
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(output, "VsaCheck.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
        } finally {
            PlayerSettings.SetScriptingBackend(target, backend);
            PlayerSettings.SetManagedStrippingLevel(target, stripping);
            AssetDatabase.SaveAssets();
        }
        var summary = report.summary;
        Debug.Log($"CHECK build {summary.result} backend={useBackend} stripping={useStripping} "
            + $"errors={summary.totalErrors}");
        EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void MakeSceneWithShellAndCheck() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var shell = new GameObject("Shell").AddComponent<SampleApp.SampleShell>();
        shell.Arguments = new[] { "--Probe:Order=inspector" };
        new GameObject("Check").AddComponent<SampleApp.Check>();
        EditorSceneManager.SaveScene(scene, Scene);
    }

    static string Argument(string name) {
        var args = Environment.GetCommandLineArgs();
        var at = Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }
}
