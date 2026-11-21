using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace StatusLab
{
    // Commands that force Editor states for the status lab. A command that blocks the main thread
    // schedules the block with delayCall, so the command returns first and the queue is free.
    static class StatusLabCommands
    {
        const string Group = "statuslab";

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr owner, string text, string caption, uint type);

        [CliGroupCommand(Group, "Hold the command queue with an async command that checks the token")]
        internal static async Task<ExitCode> Hold(CommandContext cmd,
            [CliArg("seconds", "Seconds to hold")] int seconds = 30)
        {
            var steps = seconds * 4;
            try
            {
                for (var i = 1; i <= steps; i++)
                {
                    await Task.Delay(250, cmd.CancellationToken);
                    CliProgress.Report("statuslab.hold", i + "/" + steps, progress: (float)i / steps);
                }
            }
            finally
            {
                CliProgress.Clear();
            }
            cmd.Result.WriteRecord("held " + seconds + " s");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Block the main thread with Thread.Sleep, after the command returns")]
        internal static ExitCode Freeze(CommandContext cmd,
            [CliArg("seconds", "Seconds to block")] int seconds = 90)
        {
            Later(() =>
            {
                Debug.Log("STATUSLAB_FREEZE_START");
                Thread.Sleep(seconds * 1000);
                Debug.Log("STATUSLAB_FREEZE_END");
            });
            cmd.Result.WriteRecord("freeze scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Block the main thread with a busy loop, after the command returns")]
        internal static ExitCode Spin(CommandContext cmd,
            [CliArg("seconds", "Seconds to spin")] int seconds = 90)
        {
            Later(() =>
            {
                Debug.Log("STATUSLAB_SPIN_START");
                var watch = Stopwatch.StartNew();
                long count = 0;
                while (watch.Elapsed.TotalSeconds < seconds) count++;
                Debug.Log("STATUSLAB_SPIN_END " + count);
            });
            cmd.Result.WriteRecord("spin scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Open a native Win32 message box, after the command returns")]
        internal static ExitCode NativeDialog(CommandContext cmd)
        {
            Later(() =>
            {
                Debug.Log("STATUSLAB_NATIVE_DIALOG_OPEN");
                var owner = Process.GetCurrentProcess().MainWindowHandle;
                var result = MessageBox(owner, "Forced by the status lab.", "STATUSLAB_NATIVE_DIALOG", 0);
                Debug.Log("STATUSLAB_NATIVE_DIALOG_CLOSED " + result);
            });
            cmd.Result.WriteRecord("native dialog scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Call EditorUtility.DisplayDialog, after the command returns")]
        internal static ExitCode DisplayDialog(CommandContext cmd)
        {
            Later(() =>
            {
                var result = EditorUtility.DisplayDialog("STATUSLAB_DISPLAY_DIALOG", "Forced by the status lab.", "OK", "Cancel");
                Debug.Log("STATUSLAB_DISPLAY_DIALOG_RESULT " + result);
            });
            cmd.Result.WriteRecord("display dialog scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Call SaveCurrentModifiedScenesIfUserWantsTo, after the command returns")]
        internal static ExitCode SavePrompt(CommandContext cmd)
        {
            Later(() =>
            {
                var result = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                Debug.Log("STATUSLAB_SAVE_PROMPT_RESULT " + result);
            });
            cmd.Result.WriteRecord("save prompt scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Stop the Pipeline server for some seconds, after the command returns")]
        internal static ExitCode ServerOff(CommandContext cmd,
            [CliArg("seconds", "Seconds with no server")] int seconds = 60)
        {
            Later(() => ServerSwitch.StopFor(seconds));
            cmd.Result.WriteRecord("server off scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "In Play Mode: add game code that blocks Update for some seconds")]
        internal static ExitCode PlayHang(CommandContext cmd,
            [CliArg("seconds", "Seconds to block")] int seconds = 90)
        {
            if (!EditorApplication.isPlaying) return ExitCode.Failed;
            var go = new GameObject("StatusLabPlayHang");
            go.AddComponent<global::StatusLab.PlayHang>().Seconds = seconds;
            cmd.Result.WriteRecord("play hang added");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Pause or resume Play Mode")]
        internal static ExitCode Pause(CommandContext cmd,
            [CliArg("on", "true to pause, false to resume")] bool on = true)
        {
            EditorApplication.isPaused = on;
            cmd.Result.WriteRecord(new { paused = EditorApplication.isPaused });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Mark the active scene dirty")]
        internal static ExitCode DirtyScene(CommandContext cmd)
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            cmd.Result.WriteRecord(new { scene = scene.path, dirty = scene.isDirty });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Save scenes that have a path, or discard all changes; no prompt")]
        internal static ExitCode CleanScenes(CommandContext cmd,
            [CliArg("mode", "save or discard")] string mode = "discard")
        {
            if (mode == "save")
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty && scene.path.Length > 0) EditorSceneManager.SaveScene(scene);
                }
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            cmd.Result.WriteRecord("scenes " + mode);
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Refresh the AssetDatabase, after the command returns")]
        internal static ExitCode Refresh(CommandContext cmd)
        {
            Later(AssetDatabase.Refresh);
            cmd.Result.WriteRecord("refresh scheduled");
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "Crash the Editor with an access violation, after the command returns")]
        internal static ExitCode Crash(CommandContext cmd)
        {
            Later(() =>
            {
                Debug.Log("STATUSLAB_CRASH");
                Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
            });
            cmd.Result.WriteRecord("crash scheduled");
            return ExitCode.Success;
        }

        static void Later(Action action)
        {
            EditorApplication.CallbackFunction once = null;
            once = () =>
            {
                EditorApplication.delayCall -= once;
                action();
            };
            EditorApplication.delayCall += once;
        }
    }
}
