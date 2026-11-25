using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Pipeline.Commands;

// Test commands for the lifetime experiments.
static class LabCommands
{
    const string Group = "lab";

    [CliGroupCommand(Group, "Open a modal dialog on the next Editor tick")]
    internal static ExitCode ShowDialog(CommandContext cmd,
        [CliArg("title", "Dialog title")] string title = "Lab dialog")
    {
        EditorApplication.delayCall += () =>
        {
            var ok = EditorUtility.DisplayDialog(title, "Pick a button.", "Yes", "No");
            Debug.Log("LAB_DIALOG_RESULT=" + ok);
        };
        cmd.Result.WriteRecord("scheduled");
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Block the main thread for some seconds on the next Editor tick")]
    internal static ExitCode Freeze(CommandContext cmd,
        [CliArg("seconds", "Seconds to block")] int seconds = 60)
    {
        EditorApplication.delayCall += () =>
        {
            Debug.Log("LAB_FREEZE_START");
            System.Threading.Thread.Sleep(seconds * 1000);
            Debug.Log("LAB_FREEZE_END");
        };
        cmd.Result.WriteRecord("freeze scheduled " + seconds + " s");
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Block the main thread inside this command")]
    internal static ExitCode FreezeNow(CommandContext cmd,
        [CliArg("seconds", "Seconds to block")] int seconds = 60)
    {
        Debug.Log("LAB_FREEZE_NOW_START");
        System.Threading.Thread.Sleep(seconds * 1000);
        Debug.Log("LAB_FREEZE_NOW_END");
        cmd.Result.WriteRecord("froze " + seconds + " s");
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Write a marker file from delayCall on the next Editor tick")]
    internal static ExitCode Later(CommandContext cmd,
        [CliArg("id", "Marker id")] string id = "x")
    {
        var path = System.IO.Path.GetFullPath("Temp/lab-later-" + id + ".txt");
        EditorApplication.delayCall += () => System.IO.File.WriteAllText(path, System.DateTime.UtcNow.ToString("o"));
        cmd.Result.WriteRecord("scheduled " + path);
        return ExitCode.Success;
    }

    static int s_Ticks = -1;
    static int s_DelayCalls;
    static int s_UpdateDelayCalls;

    [CliGroupCommand(Group, "Count EditorApplication.update ticks and delayCall runs")]
    internal static ExitCode Ticks(CommandContext cmd)
    {
        if (s_Ticks < 0)
        {
            s_Ticks = 0;
            EditorApplication.update += () =>
            {
                s_Ticks++;
                // Add a delayCall from the update loop, not from a command.
                if (s_Ticks % 100000 == 0) EditorApplication.delayCall += () => s_UpdateDelayCalls++;
            };
        }
        EditorApplication.delayCall += () => s_DelayCalls++;
        cmd.Result.WriteRecord("ticks=" + s_Ticks + " delayCalls=" + s_DelayCalls + " updateDelayCalls=" + s_UpdateDelayCalls
            + " pending=" + (EditorApplication.delayCall == null ? 0 : EditorApplication.delayCall.GetInvocationList().Length));
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Open a modal dialog inside this command and return the answer")]
    internal static ExitCode DialogNow(CommandContext cmd)
    {
        var start = System.DateTime.UtcNow;
        var ok = EditorUtility.DisplayDialog("Lab dialog now", "Pick a button.", "Yes", "No");
        cmd.Result.WriteRecord("answer=" + ok + " ms=" + (int)(System.DateTime.UtcNow - start).TotalMilliseconds
            + " batch=" + Application.isBatchMode);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Return the project path of this Editor")]
    internal static ExitCode Who(CommandContext cmd)
    {
        cmd.Result.WriteRecord(System.IO.Directory.GetCurrentDirectory() + " pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Add an object to the open scene so it has unsaved changes")]
    internal static ExitCode Dirty(CommandContext cmd)
    {
        var go = new GameObject("LabDirty_" + System.DateTime.Now.Ticks);
        EditorSceneManager.MarkSceneDirty(go.scene);
        cmd.Result.WriteRecord(go.scene.path + " dirty=" + go.scene.isDirty);
        return ExitCode.Success;
    }
}
