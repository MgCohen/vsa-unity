#:project ../VSA.TestSuite.Client.csproj
#nullable disable
// End-to-end check of the video recording in the Editor of the lab project.
// It examines that each stop path stops the recording and closes the file.
// Run: dotnet run --file record.cs -- <project path>
using System.Text.Json;
using VSA.TestSuite.Client;

var project = args.Length > 0 ? args[0] : @"C:\Users\matheus.cohen\Documents\Projects\pipeline-lab";
int pass = 0, fail = 0;
var videos = new List<string>();

async Task Check(string name, Func<Task<string>> body)
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        var note = await body();
        pass++;
        Console.WriteLine($"PASS  {name,-40} {watch.ElapsedMilliseconds,6} ms  {note}");
    }
    catch (Exception e)
    {
        fail++;
        Console.WriteLine($"FAIL  {name,-40} {watch.ElapsedMilliseconds,6} ms  {e.GetType().Name}: {e.Message}");
    }
}

using var game = await Game.AttachEditor(project);
await game.EnterPlayMode();

// Examines the last recording: its name, its stop reason, and a complete MP4 file.
async Task<string> ExpectLast(string name, string reason)
{
    var s = await game.RecordingStatus();
    if (s.GetProperty("Current").ValueKind != JsonValueKind.Null) throw new Exception("A recording still runs: " + s.GetProperty("Current"));
    var last = s.GetProperty("Last");
    if (last.ValueKind == JsonValueKind.Null) throw new Exception("No recording");
    var gotName = last.GetProperty("Name").GetString();
    var gotReason = last.GetProperty("Reason").GetString();
    if (gotName != name || gotReason != reason) throw new Exception($"Last is {gotName}/{gotReason}, expected {name}/{reason}");
    var file = last.GetProperty("File").GetString();
    var bytes = File.Exists(file) ? new FileInfo(file).Length : 0;
    if (bytes == 0) throw new Exception("No video file: " + file);
    // The encoder writes the "moov" index at the end. Without it, players cannot open the file.
    var data = File.ReadAllBytes(file);
    if (System.Text.Encoding.ASCII.GetString(data).IndexOf("moov", StringComparison.Ordinal) < 0)
        throw new Exception("The MP4 file is not closed (no moov box): " + file);
    videos.Add(file);
    return $"{reason}, {last.GetProperty("Seconds")} s, {bytes / 1024} KB";
}

await Check("recorder is ready", async () =>
{
    var s = await game.RecordingStatus();
    var why = s.GetProperty("Unavailable");
    if (why.ValueKind != JsonValueKind.Null) throw new Exception(why.GetString());
    return s.GetProperty("Recorder").GetString();
});

await Check("passed script records a video", async () =>
{
    await game.RunScript("lab_demo");
    return await ExpectLast("lab_demo", "Stopped");
});

await Check("failed script stops the video", async () =>
{
    try { await game.RunScript("lab_fail"); throw new Exception("Expected AssertFailed"); }
    catch (ActionFailedException e) when (e.Code == "AssertFailed") { }
    return await ExpectLast("lab_fail", "StepFailed");
});

await Check("command time limit stops the video", async () =>
{
    var old = game.CommandTimeoutMs;
    game.CommandTimeoutMs = 3000;
    try { await game.RunScript("lab_hang"); throw new Exception("Expected a time-out"); }
    catch (Exception e) when (e is InstanceException || e is CommandFailedException) { }
    finally { game.CommandTimeoutMs = old; }
    // The cancel reaches the script at its next frame.
    await Task.Delay(1000);
    return await ExpectLast("lab_hang", "StepFailed");
});

await Check("client handle stops on exception", async () =>
{
    try
    {
        await using var rec = await game.Record("handle");
        await game.WaitSeconds(0.5);
        throw new InvalidOperationException("test failure");
    }
    catch (InvalidOperationException) { }
    return await ExpectLast("handle", "Stopped");
});

await Check("time limit stops a forgotten video", async () =>
{
    await game.StartRecording("forgotten", maxSeconds: 2);
    await Task.Delay(3500);
    return await ExpectLast("forgotten", "MaxTime");
});

await Check("new recording replaces a running one", async () =>
{
    await game.StartRecording("first");
    await game.WaitSeconds(0.3);
    await game.StartRecording("second");
    await game.WaitSeconds(0.3);
    await game.StopRecording();
    return await ExpectLast("second", "Stopped");
});

await Check("stop with no recording does nothing", async () =>
{
    var r = await game.StopRecording();
    return "returns the last: " + r.GetProperty("Name").GetString();
});

await Check("Play Mode exit stops the video", async () =>
{
    await game.StartRecording("exit");
    await game.WaitSeconds(0.5);
    await game.ExitPlayMode();
    await Task.Delay(2000);
    return await ExpectLast("exit", "PlayModeExit");
});

Console.WriteLine($"\n{pass} passed, {fail} failed");
foreach (var v in videos) Console.WriteLine("video: " + v);
return fail == 0 ? 0 : 1;
