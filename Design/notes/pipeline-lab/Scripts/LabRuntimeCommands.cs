using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Unity.Pipeline.Commands;

// Runtime test commands: they run in Play Mode and in a development player.
static partial class LabRuntimeCommands
{
    const string Group = "labrt";

    [CliGroupCommand(Group, "Return the frame count, platform and process id")]
    internal static ExitCode Ping(CommandContext cmd)
    {
        cmd.Result.WriteRecord("frame=" + Time.frameCount
            + " platform=" + Application.platform
            + " pid=" + System.Diagnostics.Process.GetCurrentProcess().Id
            + " focus=" + Application.isFocused);
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Make a full-screen button and a key counter; return the button center")]
    internal static ExitCode SetupUi(CommandContext cmd)
    {
        LabUi.Create();
        cmd.Result.WriteRecord("x=" + (Screen.width / 2) + " y=" + (Screen.height / 2));
        return ExitCode.Success;
    }
}

// Builds the lab UI: an EventSystem, a canvas, a button that counts clicks, and a label.
static class LabUi
{
    public static void Create()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("LabEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("LabCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var buttonGo = new GameObject("LabButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(canvasGo.transform, false);
        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.25f);
        rect.anchorMax = new Vector2(0.75f, 0.75f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        buttonGo.GetComponent<Button>().onClick.AddListener(() => LabCounter.Clicks++);

        var labelGo = new GameObject("LabLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(canvasGo.transform, false);
        var label = labelGo.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.raycastTarget = false;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.85f); lr.anchorMax = new Vector2(1f, 1f); lr.offsetMin = lr.offsetMax = Vector2.zero;

        canvasGo.AddComponent<LabCounter>().Label = label;
    }
}

static partial class LabRuntimeCommands
{

    [CliGroupCommand(Group, "Return the click and key counts")]
    internal static ExitCode State(CommandContext cmd)
    {
        var kb = Keyboard.current;
        var names = new System.Collections.Generic.List<string>();
        foreach (var d in InputSystem.devices) names.Add(d.name + "#" + d.deviceId + (d is Keyboard k && k.spaceKey.isPressed ? "(space down)" : ""));
        cmd.Result.WriteRecord("clicks=" + LabCounter.Clicks + " spaces=" + LabCounter.Spaces + " updates=" + LabCounter.Updates
            + " frame=" + Time.frameCount + " current=" + (kb == null ? "none" : kb.name + "#" + kb.deviceId)
            + " devices=" + string.Join(",", names));
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Call Application.Quit on the next frame")]
    internal static ExitCode Quit(CommandContext cmd)
    {
        Debug.Log("LAB_QUIT_REQUESTED");
        LabCounter.QuitNextFrame = true;
        if (Object.FindAnyObjectByType<LabCounter>() == null) new GameObject("LabQuitter").AddComponent<LabCounter>();
        cmd.Result.WriteRecord("quit requested");
        return ExitCode.Success;
    }
}

sealed class LabCounter : MonoBehaviour
{
    public static int Clicks;
    public static int Spaces;
    public static bool QuitNextFrame;

    public static int Updates;
    public static float ShowButtonAt = -1;
    public Text Label;

    void Update()
    {
        Updates++;
        if (Label != null) Label.text = "clicks=" + Clicks;
        if (ShowButtonAt > 0 && Time.realtimeSinceStartup >= ShowButtonAt)
        {
            ShowButtonAt = -1;
            transform.Find("LabButton")?.gameObject.SetActive(true);
        }
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Spaces++;
        if (QuitNextFrame) { QuitNextFrame = false; Application.Quit(); }
    }
}
