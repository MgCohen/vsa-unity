using System.Threading.Tasks;
using VSA.TestSuite;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Lab commands to test the test-suite core: an inside caller, the lazy start and failure cases.
static class LabSuiteCommands
{
    const string Group = "labsuite";

    [CliGroupCommand(Group, "Tell whether the action runtime is built")]
    internal static ExitCode Info(CommandContext cmd)
    {
        var drag = Object.FindAnyObjectByType<LabDraggable>();
        cmd.Result.Write(new
        {
            built = ActionRuntime.IsBuilt, playing = Application.isPlaying, frame = Time.frameCount, clicks = LabCounter.Clicks,
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            random = Random.Range(0, 1000000),
            drags = drag == null ? -1 : drag.Drags,
            dragX = drag == null ? -1 : Mathf.Round(((RectTransform)drag.transform).anchoredPosition.x),
            holdMs = drag == null ? -1 : drag.LastHoldMs,
            dropped = drag == null ? null : drag.DroppedOn,
        });
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Forget the action runtime, to test the lazy start")]
    internal static ExitCode Forget(CommandContext cmd)
    {
        ActionRuntime.Reset();
        cmd.Result.Write(new { built = ActionRuntime.IsBuilt });
        return ExitCode.Success;
    }

    // A script inside Unity: it calls the runtime directly, not the CLI.
    [CliGroupCommand(Group, "Run a C# script inside Unity: click the lab button two times")]
    internal static async Task<ExitCode> Inside(CommandContext cmd)
    {
        var run = ActionRuntime.GetOrBuild();
        int before = LabCounter.Clicks;
        var a = await run.RunStep(new ClickObject("LabCanvas/LabButton"), cmd.CancellationToken);
        var b = await run.RunStep(new ClickObject("LabCanvas/LabButton"), cmd.CancellationToken);
        cmd.Result.Write(new { clicksAdded = LabCounter.Clicks - before, first = a, second = b });
        return LabCounter.Clicks - before == 2 ? ExitCode.Success : ExitCode.Failed;
    }

    [CliGroupCommand(Group, "Put a full-screen image over the lab button, or remove it")]
    internal static ExitCode Cover(CommandContext cmd, [CliArg("on", "true = add the cover")] bool on = true)
    {
        var canvas = GameObject.Find("LabCanvas");
        if (canvas == null) { cmd.Log.Error("Run labrt.setup_ui first"); return ExitCode.Failed; }
        var old = canvas.transform.Find("LabCover");
        if (old != null) Object.Destroy(old.gameObject);
        if (on)
        {
            var cover = new GameObject("LabCover", typeof(RectTransform), typeof(Image));
            cover.transform.SetParent(canvas.transform, false);
            var r = cover.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            cover.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        }
        cmd.Result.Write(new { cover = on });
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Hide the lab button now and show it again after a delay")]
    internal static ExitCode ShowLater(CommandContext cmd, [CliArg("ms", "Delay in milliseconds")] int ms = 1000)
    {
        var canvas = GameObject.Find("LabCanvas");
        if (canvas == null) { cmd.Log.Error("Run labrt.setup_ui first"); return ExitCode.Failed; }
        canvas.transform.Find("LabButton").gameObject.SetActive(false);
        LabCounter.ShowButtonAt = Time.realtimeSinceStartup + ms / 1000f;
        cmd.Result.Write(new { showAfterMs = ms });
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Add a draggable square at the lower left and a drop target at the lower right")]
    internal static ExitCode SetupDrag(CommandContext cmd)
    {
        var canvas = GameObject.Find("LabCanvas");
        if (canvas == null) { cmd.Log.Error("Run labrt.setup_ui first"); return ExitCode.Failed; }
        AddDragTargets(canvas);
        cmd.Result.Write(new { ok = true });
        return ExitCode.Success;
    }

    /// <summary>Adds the draggable square and the drop target. Test scripts call it too.</summary>
    internal static void AddDragTargets(GameObject canvas)
    {
        Square(canvas, "LabDrop", new Vector2(0.9f, 0.1f), new Color(0.2f, 0.6f, 0.3f)).AddComponent<LabDrop>();
        Square(canvas, "LabDrag", new Vector2(0.1f, 0.1f), new Color(0.8f, 0.4f, 0.1f)).AddComponent<LabDraggable>();
    }

    static GameObject Square(GameObject canvas, string name, Vector2 anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.sizeDelta = new Vector2(60, 60);
        r.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    [CliGroupCommand(Group, "Show or hide the lab button")]
    internal static ExitCode Show(CommandContext cmd, [CliArg("on", "true = active")] bool on = true)
    {
        var canvas = GameObject.Find("LabCanvas");
        if (canvas == null) { cmd.Log.Error("Run labrt.setup_ui first"); return ExitCode.Failed; }
        canvas.transform.Find("LabButton").gameObject.SetActive(on);
        cmd.Result.Write(new { active = on });
        return ExitCode.Success;
    }
}

// Moves with the pointer while dragged. Counts drags and measures the last press time.
sealed class LabDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public int Drags;
    public int LastHoldMs = -1;
    public string DroppedOn;
    float downAt;
    Graphic graphic;

    void Awake() => graphic = GetComponent<Graphic>();
    public void OnPointerDown(PointerEventData e) => downAt = Time.realtimeSinceStartup;
    public void OnPointerUp(PointerEventData e) => LastHoldMs = Mathf.RoundToInt((Time.realtimeSinceStartup - downAt) * 1000);
    public void OnBeginDrag(PointerEventData e) { graphic.raycastTarget = false; DroppedOn = null; }
    public void OnDrag(PointerEventData e) => ((RectTransform)transform).anchoredPosition += e.delta / GetComponentInParent<Canvas>().scaleFactor;
    public void OnEndDrag(PointerEventData e) { graphic.raycastTarget = true; Drags++; }
}

sealed class LabDrop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData e)
    {
        var d = e.pointerDrag == null ? null : e.pointerDrag.GetComponent<LabDraggable>();
        if (d != null) d.DroppedOn = name;
    }
}
