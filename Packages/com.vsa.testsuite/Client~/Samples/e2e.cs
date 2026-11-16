#:project ../VSA.TestSuite.Client.csproj
// End-to-end check of com.vsa.testsuite against the lab project.
// Run: dotnet run --file e2e.cs -- <project path> [<lab scene name>]
// The project needs the lab commands labrt.* and labsuite.* (vsa-unity: Assets/TestSuiteLab).
#nullable disable
using System.Text.Json;
using VSA.TestSuite.Client;

var project = args.Length > 0 ? args[0] : @"C:\Users\matheus.cohen\Documents\Projects\pipeline-lab";
// The scene that the reset and load checks use. It must be in the build scene list.
var scene = args.Length > 1 ? args[1] : "SampleScene";
int pass = 0, fail = 0;

async Task Check(string name, Func<Task<string>> body)
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        var note = await body();
        pass++;
        Console.WriteLine($"PASS  {name,-36} {watch.ElapsedMilliseconds,6} ms  {note}");
    }
    catch (Exception e)
    {
        fail++;
        Console.WriteLine($"FAIL  {name,-36} {watch.ElapsedMilliseconds,6} ms  {e.GetType().Name}: {e.Message}");
    }
}

async Task<string> ExpectFail(Func<Task> body, string code)
{
    try { await body(); }
    catch (ActionFailedException e) when (e.Code == code) { return code + ": " + e.Reason; }
    throw new Exception("Expected " + code);
}

using var game = await Game.AttachEditor(project);
Console.WriteLine("Attached: " + game.Transport.Instance);

await Check("health ready", async () => (await game.Health.Probe()).ToString());
await Check("enter play mode", async () => { await game.EnterPlayMode(); return "ticking"; });
await Check("set up lab UI", async () => (await game.Command("labrt.setup_ui")).ToString());
// Static fields keep their values if the project enters Play Mode with no domain reload.
async Task<int> Clicks() => (await game.Command("labsuite.info")).GetProperty("clicks").GetInt32();
int start = await Clicks();
await Check("click object", async () =>
{
    var r = await game.ClickObject("LabCanvas/LabButton");
    return "hit " + r.GetProperty("Hit").GetString();
});
await Check("assert text after 1 click", async () => (await game.AssertText("LabCanvas/LabLabel", "clicks=" + (start + 1))).ToString());
await Check("blocked is reported", () => ExpectFail(async () =>
{
    await game.Command("labsuite.cover", "--on", "true");
    try { await game.ClickObject("LabCanvas/LabButton"); }
    finally { await game.Command("labsuite.cover", "--on", "false"); }
}, "Blocked"));
await Check("not found is reported", () => ExpectFail(() => game.ClickObject("LabCanvas/Nope"), "NotFound"));
await Check("wait for object shown later", async () =>
{
    await game.Command("labsuite.show_later", "--ms", "1000");
    var r = await game.WaitForObject("LabCanvas/LabButton", timeoutMs: 5000);
    return r.GetProperty("Ms").ToString() + " ms";
});
await Check("wait time-out is reported", () => ExpectFail(() => game.WaitForObject("LabCanvas/Nope", timeoutMs: 500), "TimedOut"));
await Check("unknown command fails clearly", async () =>
{
    try { await game.Command("labsuite.no_such_command"); }
    catch (CommandFailedException e) { return e.Exec.ToString(); }
    throw new Exception("Expected CommandFailedException");
});
await Check("10 clicks, then count", async () =>
{
    var times = new List<double>();
    for (int i = 0; i < 10; i++)
    {
        var w = System.Diagnostics.Stopwatch.StartNew();
        await game.ClickObject("LabCanvas/LabButton");
        times.Add(w.Elapsed.TotalMilliseconds);
    }
    await game.AssertText("LabCanvas/LabLabel", "clicks=" + (start + 11));
    return "ms: " + string.Join(", ", times.Select(t => t.ToString("0")));
});
await Check("long async action stays Ready", async () =>
{
    var wait = game.WaitSeconds(3);
    await Task.Delay(1000);
    var during = await game.Health.Probe();
    await wait;
    if (during.State != HealthState.Ready) throw new Exception("During the wait: " + during);
    return $"{during.State} in {during.Ms} ms";
});
async Task<JsonElement> Info() => await game.Command("labsuite.info");
await Check("hold object 400 ms", async () =>
{
    await game.Command("labsuite.setup_drag");
    await game.HoldObject("LabCanvas/LabDrag", 400);
    int held = (await Info()).GetProperty("holdMs").GetInt32();
    if (held < 380 || held > 700) throw new Exception("Held " + held + " ms");
    return "game measured " + held + " ms";
});
await Check("drag object onto target", async () =>
{
    var r = await game.DragObject("LabCanvas/LabDrag", "LabCanvas/LabDrop", 12);
    var info = await Info();
    if (info.GetProperty("drags").GetInt32() != 1) throw new Exception("Drags: " + info.GetProperty("drags"));
    if (info.GetProperty("dropped").GetString() != "LabDrop") throw new Exception("Dropped on: " + info.GetProperty("dropped"));
    return $"x now {info.GetProperty("dragX")}, frames {r.GetProperty("FrameAfter").GetInt32() - r.GetProperty("FrameBefore").GetInt32()}";
});
float[] Point(JsonElement found) => found.GetProperty("ScreenPoint").EnumerateArray().Select(v => v.GetSingle()).ToArray();
await Check("find object gives a screen point", async () =>
{
    var p = Point(await game.FindObject("LabCanvas/LabButton"));
    return $"({p[0]}, {p[1]})";
});
await Check("click screen at the button", async () =>
{
    var p = Point(await game.FindObject("LabCanvas/LabButton"));
    int before = await Clicks();
    var r = await game.ClickScreen(p[0], p[1]);
    if (await Clicks() != before + 1) throw new Exception("The count did not change");
    return "hit " + r.GetProperty("Hit").GetString();
});
await Check("assert state Active and Inactive", async () =>
{
    await game.AssertState("LabCanvas/LabButton", "Active");
    await game.Command("labsuite.show", "--on", "false");
    try { await game.AssertState("LabCanvas/LabButton", "Inactive"); }
    finally { await game.Command("labsuite.show", "--on", "true"); }
    return "Active, then Inactive";
});
await Check("hold screen 300 ms", async () =>
{
    var p = Point(await game.FindObject("LabCanvas/LabDrag"));
    await game.HoldScreen(p[0], p[1], 300);
    int held = (await Info()).GetProperty("holdMs").GetInt32();
    if (held < 280 || held > 600) throw new Exception("Held " + held + " ms");
    return "game measured " + held + " ms";
});
await Check("drag screen moves the square", async () =>
{
    var p = Point(await game.FindObject("LabCanvas/LabDrag"));
    var before = await Info();
    await game.DragScreen(p[0], p[1], p[0] - 100, p[1] + 50, 10);
    var after = await Info();
    if (after.GetProperty("drags").GetInt32() != before.GetProperty("drags").GetInt32() + 1) throw new Exception("No drag");
    double dx = after.GetProperty("dragX").GetDouble() - before.GetProperty("dragX").GetDouble();
    if (Math.Abs(dx + 100) > 2) throw new Exception("Moved " + dx + " px, expected -100");
    return "moved " + dx + " px";
});
await Check("wait frames", async () =>
{
    var r = await game.WaitFrames(5);
    return r.ToString().Replace("\n", " ").Replace("  ", "");
});
await Check("game info in the Editor", async () =>
{
    var i = await game.Info();
    if (!i.GetProperty("IsEditor").GetBoolean() || !i.GetProperty("IsPlaying").GetBoolean()) throw new Exception(i.ToString());
    return $"{i.GetProperty("Platform")} scene={i.GetProperty("Scene")}";
});
await Check("touch overlay is found and can hide", async () =>
{
    await game.FindObject("VSA.TouchOverlay");
    var off = await game.ShowTouches(false);
    var on = await game.ShowTouches(true);
    if (off.GetBoolean() || !on.GetBoolean()) throw new Exception($"off={off} on={on}");
    return "found in DontDestroyOnLoad, off and on";
});
await Check("lazy start builds the runtime again", async () =>
{
    var forgot = await game.Command("labsuite.forget");
    if (forgot.GetProperty("built").GetBoolean()) throw new Exception("Still built");
    await game.Info();
    if (!(await Info()).GetProperty("built").GetBoolean()) throw new Exception("Not built after a command");
    return "forgotten, then built by game.info";
});
await Check("caller inside Unity runs steps", async () =>
{
    var r = await game.Command("labsuite.inside");
    return "clicks added: " + r.GetProperty("clicksAdded").GetInt32();
});
await Check("reset of an unsaved scene fails clearly", async () =>
{
    var scene = (await Info()).GetProperty("scene").GetString();
    if (scene != "") return "skipped: the active scene is '" + scene + "'";
    return await ExpectFail(() => game.Reset(), "Failed");
});
await Check("reset loads the scene", async () =>
{
    var r = await game.Reset(scene);
    await game.AssertState("LabCanvas", "Missing");
    return r.GetProperty("Scene").GetString() + " in " + r.GetProperty("Ms") + " ms";
});
await Check("reset with a seed repeats random values", async () =>
{
    await game.Reset(scene, seed: 42);
    int a = (await Info()).GetProperty("random").GetInt32();
    await game.Reset(seed: 42);
    int b = (await Info()).GetProperty("random").GetInt32();
    if (a != b) throw new Exception(a + " != " + b);
    return "both " + a;
});
await Check("load scene by name", async () => (await game.LoadScene(scene)).GetProperty("Scene").GetString());
await Check("unknown scene is NotFound", () => ExpectFail(() => game.LoadScene("NoSuchScene"), "NotFound"));
await Check("list scripts", async () => string.Join(", ", (await game.ListScripts()).EnumerateArray().Select(x => x.GetString())));
await Check("script lab_click passes", async () =>
{
    var r = await game.RunScript("lab_click");
    return $"{r.GetProperty("Steps")} steps in {r.GetProperty("Ms")} ms";
});
await Check("script lab_fail stops at the assert", () => ExpectFail(() => game.RunScript("lab_fail"), "AssertFailed"));
await Check("unknown script is NotFound", () => ExpectFail(() => game.RunScript("no_such_script"), "NotFound"));
await Check("exit play mode", async () => { await game.ExitPlayMode(); return ""; });

// Health: freeze the main thread, then probe from outside.
await Check("frozen main thread is Frozen", async () =>
{
    var freeze = game.Transport.Exec(new[] { "lab.freeze_now", "--seconds", "6" }, 20000);
    await Task.Delay(1500);
    var during = await game.Health.Probe();
    var frozeFor = await freeze;
    if (!frozeFor.Success) throw new Exception("The freeze command failed: " + frozeFor);
    var after = await game.Health.Probe();
    if (during.State != HealthState.Frozen) throw new Exception("During the freeze: " + during);
    if (after.State != HealthState.Ready) throw new Exception("After the freeze: " + after);
    return $"during: {during.State} in {during.Ms} ms, after: {after.State}";
});

Console.WriteLine($"\n{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
