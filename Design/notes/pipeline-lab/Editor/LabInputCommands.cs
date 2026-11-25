using UnityEngine.InputSystem;
using Unity.Pipeline.Commands;

// Test commands for Input System focus settings in the Editor.
static class LabInputCommands
{
    const string Group = "labin";

    [CliGroupCommand(Group, "Read the Input System focus settings")]
    internal static ExitCode Get(CommandContext cmd)
    {
        var s = InputSystem.settings;
        cmd.Result.WriteRecord("backgroundBehavior=" + s.backgroundBehavior
            + " editorInputBehaviorInPlayMode=" + s.editorInputBehaviorInPlayMode);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Queue a Space key state; no manual InputSystem.Update")]
    internal static ExitCode KeyState(CommandContext cmd,
        [CliArg("down", "true = pressed, false = released")] bool down = true,
        [CliArg("update", "Also call InputSystem.Update() at once")] bool update = false)
    {
        var kb = Keyboard.current;
        if (kb == null) { cmd.Log.Error("No keyboard"); return ExitCode.Failed; }
        var state = down ? new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Space) : new UnityEngine.InputSystem.LowLevel.KeyboardState();
        InputSystem.QueueStateEvent(kb, state);
        if (update) InputSystem.Update();
        cmd.Result.WriteRecord("queued down=" + down + " update=" + update);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Focus the Game view window")]
    internal static ExitCode FocusGame(CommandContext cmd)
    {
        UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
        var w = UnityEditor.EditorWindow.focusedWindow;
        cmd.Result.WriteRecord("focusedWindow=" + (w == null ? "none" : w.GetType().Name)
            + " appFocused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Set the Input System focus settings for this session (not saved)")]
    internal static ExitCode Set(CommandContext cmd,
        [CliArg("background", "ResetAndDisableNonBackgroundDevices | ResetAndDisableAllDevices | IgnoreFocus")] string background = "IgnoreFocus",
        [CliArg("editor", "PointersAndKeyboardsRespectGameViewFocus | AllDevicesRespectGameViewFocus | AllDeviceInputAlwaysGoesToGameView")] string editor = "AllDeviceInputAlwaysGoesToGameView")
    {
        var s = InputSystem.settings;
        s.backgroundBehavior = (InputSettings.BackgroundBehavior)System.Enum.Parse(typeof(InputSettings.BackgroundBehavior), background);
        s.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)System.Enum.Parse(typeof(InputSettings.EditorInputBehaviorInPlayMode), editor);
        cmd.Result.WriteRecord("backgroundBehavior=" + s.backgroundBehavior
            + " editorInputBehaviorInPlayMode=" + s.editorInputBehaviorInPlayMode);
        return ExitCode.Success;
    }
}
