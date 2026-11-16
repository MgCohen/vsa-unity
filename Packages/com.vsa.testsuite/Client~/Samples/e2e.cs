#:project ../VSA.TestSuite.Client.csproj
// End-to-end check of com.vsa.testsuite against the lab project.
// Run: dotnet run --file e2e.cs -- <project path>
// The project needs the lab commands labrt.* and labsuite.* (see pipeline-lab).
using System.Text.Json;
using VSA.TestSuite.Client;

var project = args.Length > 0 ? args[0] : @"C:\Users\matheus.cohen\Documents\Projects\pipeline-lab";
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
await Check("exit play mode", async () => { await game.ExitPlayMode(); return ""; });

// Health: freeze the main thread, then probe from outside.
await Check("frozen main thread is Frozen", async () =>
{
    var freeze = game.Transport.Exec(new[] { "lab.freeze_now", "--seconds", "6" }, 20000);
    await Task.Delay(1500);
    var during = await game.Health.Probe();
    await freeze;
    var after = await game.Health.Probe();
    if (during.State != HealthState.Frozen) throw new Exception("During the freeze: " + during);
    if (after.State != HealthState.Ready) throw new Exception("After the freeze: " + after);
    return $"during: {during.State} in {during.Ms} ms, after: {after.State}";
});

Console.WriteLine($"\n{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
