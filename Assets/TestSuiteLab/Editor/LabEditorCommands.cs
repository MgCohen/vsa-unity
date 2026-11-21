using Unity.Pipeline.Commands;
using UnityEngine;

// Editor lab commands for the health probe checks.
static class LabEditorCommands
{
    const string Group = "lab";

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
}
