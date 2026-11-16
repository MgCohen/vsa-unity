#:project ../VSA.TestSuite.Client.csproj
#nullable disable
// End-to-end check of com.vsa.testsuite in a Windows development player of the lab project.
// Run: dotnet run --file player.cs [-- <path of Lab.exe> ["<player arguments>"]]
// Default: Build/TestSuiteLab/Lab.exe in the Unity project above the current folder.
// Use "-batchmode" as the player arguments to start the player with no window, so it takes no focus.
using System.Text.Json;
using VSA.TestSuite.Client;

var exe = args.Length > 0 ? args[0] : Path.Combine(ProjectPaths.FindProjectRoot() ?? ".", "Build", "TestSuiteLab", "Lab.exe");
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
    catch (ActionFailedException e) when (e.Code == code) { return code + ", evidence in " + e.Folder; }
    throw new Exception("Expected " + code);
}

var launch = System.Diagnostics.Stopwatch.StartNew();
var playerArgs = args.Length > 1 ? args[1] : "-screen-fullscreen 0 -screen-width 960 -screen-height 540";
using var game = await Game.LaunchPlayer(exe, playerArgs);
Console.WriteLine($"Launched: {game.Transport.Instance} in {launch.ElapsedMilliseconds} ms");

try
{
    await Check("health ready", async () => (await game.Health.Probe()).ToString());
    await Check("game info", async () =>
    {
        var i = await game.Info();
        return $"{i.GetProperty("Platform")} editor={i.GetProperty("IsEditor")} scene={i.GetProperty("Scene")}";
    });
    await Check("set up lab UI", async () => { await game.Command("labrt.setup_ui"); await game.Command("labsuite.setup_drag"); return ""; });
    int start = (await game.Command("labsuite.info")).GetProperty("clicks").GetInt32();
    await Check("click object", async () =>
    {
        var r = await game.ClickObject("LabCanvas/LabButton");
        return $"hit {r.GetProperty("Hit")} via {r.GetProperty("Adapter")}";
    });
    await Check("assert text", async () => (await game.AssertText("LabCanvas/LabLabel", "clicks=" + (start + 1))).ToString());
    await Check("drag object onto target", async () =>
    {
        await game.DragObject("LabCanvas/LabDrag", "LabCanvas/LabDrop");
        var dropped = (await game.Command("labsuite.info")).GetProperty("dropped").GetString();
        if (dropped != "LabDrop") throw new Exception("Dropped on " + dropped);
        return "dropped on LabDrop";
    });
    await Check("hold object 300 ms", async () =>
    {
        await game.HoldObject("LabCanvas/LabDrag", 300);
        int held = (await game.Command("labsuite.info")).GetProperty("holdMs").GetInt32();
        if (held < 280 || held > 600) throw new Exception("Held " + held + " ms");
        return "game measured " + held + " ms";
    });
    await Check("click screen at the button", async () =>
    {
        var p = (await game.FindObject("LabCanvas/LabButton")).GetProperty("ScreenPoint").EnumerateArray().Select(v => v.GetSingle()).ToArray();
        var r = await game.ClickScreen(p[0], p[1]);
        return "hit " + r.GetProperty("Hit").GetString();
    });
    await Check("touch overlay is found", async () =>
    {
        await game.FindObject("VSA.TouchOverlay");
        return "found in DontDestroyOnLoad";
    });
    await Check("failed assert saves evidence", () => ExpectFail(() => game.AssertText("LabCanvas/LabLabel", "wrong"), "AssertFailed"));
    await Check("script lab_click passes", async () =>
    {
        var r = await game.RunScript("lab_click");
        return $"{r.GetProperty("Steps")} steps in {r.GetProperty("Ms")} ms";
    });
    await Check("recording gives NoDevice", () => ExpectFail(() => game.StartRecording("player"), "NoDevice"));
    await Check("10 clicks", async () =>
    {
        var times = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            var w = System.Diagnostics.Stopwatch.StartNew();
            await game.ClickObject("LabCanvas/LabButton");
            times.Add(w.Elapsed.TotalMilliseconds);
        }
        return "ms: " + string.Join(", ", times.Select(t => t.ToString("0")));
    });
}
finally
{
    game.StopPlayer();
    Console.WriteLine("Player stopped: " + (game.OwnedProcess == null));
}

Console.WriteLine($"\n{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
