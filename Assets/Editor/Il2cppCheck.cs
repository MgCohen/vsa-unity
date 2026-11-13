using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using VSA.Unity;

// Builds the IL2CPP player of the check (managed stripping High). il2cpp-check.ps1 calls Build, then runs the player.
//   Unity.exe -batchmode -projectPath . -executeMethod Il2cppCheck.Build -checkOutput <folder>
public static class Il2cppCheck {
    const string Scene = "Assets/Check.unity";

    public static void Build() {
        var output = Argument("-checkOutput") ?? Path.GetFullPath("Il2cppCheck/Player");
        MakeScene();
        var target = NamedBuildTarget.Standalone;
        var backend = PlayerSettings.GetScriptingBackend(target);
        var stripping = PlayerSettings.GetManagedStrippingLevel(target);
        BuildReport report;
        try {
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
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
        Debug.Log($"CHECK build {report.summary.result} backend=IL2CPP stripping=High errors={report.summary.totalErrors}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    // One scene: VsaHost with an empty App field (the app scan finds SampleApp), and the Check view.
    static void MakeScene() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("VsaHost").AddComponent<VsaHost>();
        new GameObject("Check").AddComponent<SampleApp.Check>();
        EditorSceneManager.SaveScene(scene, Scene);
    }

    static string Argument(string name) {
        var args = Environment.GetCommandLineArgs();
        var at = Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }
}
