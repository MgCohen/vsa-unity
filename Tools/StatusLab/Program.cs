using System.Text.Json;
using StatusLab;

// Spike CLI of the status lab. Output: one JSON envelope on stdout; progress on stderr.
//   statuslab start | stop
//   statuslab sample [--seconds N]
//   statuslab playbook A|B [--runs N] [--steps A1,A3] [--full-import]
//   statuslab replay <file or folder>
var cancelSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelSource.Cancel(); };
var cancel = cancelSource.Token;

string Option(string name, string fallback = null)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}

string FindProject()
{
    var dir = new DirectoryInfo(Option("--project", Directory.GetCurrentDirectory()));
    for (; dir != null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt"))) return dir.FullName;
    throw new DirectoryNotFoundException("No Unity project above the current folder.");
}

// Finds a file in the folder of the program or a folder above it.
string FindUp(string name)
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, name))) return Path.Combine(dir.FullName, name);
    return Path.Combine(AppContext.BaseDirectory, name);
}

int Envelope(string command, bool success, object data, string error = null)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        success,
        command,
        data,
        errors = error == null ? Array.Empty<object>() : new object[] { new { code = "Failed", message = error } },
        warnings = Array.Empty<object>(),
    }, Recorder.Options));
    return success ? 0 : 6;
}

var verb = args.Length > 0 ? args[0] : "help";
var project = verb == "replay" ? null : FindProject();
var resultsRoot = Path.Combine(Path.GetDirectoryName(FindUp("StatusLab.csproj")), "results");

switch (verb)
{
    case "start":
    {
        var target = new Target { ProjectPath = project };
        await EditorLauncher.Start(target, cancel);
        return Envelope(verb, true, new { pid = target.Pid, logPath = target.LogPath, editorId = target.EditorId });
    }
    case "stop":
    {
        var target = new Target { ProjectPath = project };
        if (!EditorLauncher.TryLoadOwn(target)) return Envelope(verb, true, new { stopped = false, reason = "no own Editor runs" });
        var control = new EditorControl();
        await control.Quit(target, cancel);
        var until = DateTime.UtcNow.AddSeconds(30);
        while (!target.Process.HasExited && DateTime.UtcNow < until) await Task.Delay(500, cancel);
        if (!target.Process.HasExited) control.Stop(target);
        return Envelope(verb, true, new { stopped = true, pid = target.Pid, exitCode = target.Process.HasExited ? target.Process.ExitCode : (int?)null });
    }
    case "sample":
    {
        var target = new Target { ProjectPath = project };
        EditorLauncher.TryLoadOwn(target);
        var seconds = int.Parse(Option("--seconds", "20"));
        var path = Path.Combine(resultsRoot, "sample-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ndjson");
        using var session = new LiveSession(target, path);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        stop.CancelAfter(seconds * 1000);
        var loop = session.Run(stop.Token);
        string last = null;
        while (!loop.IsCompleted)
        {
            await Task.Delay(250);
            var r = session.Last;
            var s = session.LastSnapshot;
            if (r == null) continue;
            var line = $"{s.T,7} {r,-22} status={s.StatusHttp}/{s.Status} probe={s.ProbeHttp}/{s.ProbeMs} beacon={(s.BeaconOwn ? s.BeaconAgeS?.ToString() : "-")} cpu={s.ProcCpuPct} hung={s.Hung} dlg={s.DialogTitle}";
            if (line != last) Console.Error.WriteLine(line);
            last = line;
        }
        try { await loop; } catch (OperationCanceledException) { }
        return Envelope(verb, true, new { recording = Path.GetFullPath(path), last = session.Last?.ToString() });
    }
    case "playbook":
    {
        var which = args.Length > 1 ? args[1].ToUpperInvariant() : "A";
        var runs = int.Parse(Option("--runs", "1"));
        var steps = Option("--steps")?.Split(',').ToHashSet();
        var folder = Path.Combine(resultsRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + which);
        var target = new Target { ProjectPath = project };
        EditorLauncher.TryLoadOwn(target);
        var files = new List<string>();
        for (var run = 1; run <= runs && !cancel.IsCancellationRequested; run++)
        {
            var path = Path.Combine(folder, which + "-r" + run + ".ndjson");
            files.Add(Path.GetFullPath(path));
            using var session = new LiveSession(target, path);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            var loop = session.Run(stop.Token);
            PlaybookRunner runner = which switch
            {
                "B" => new PlaybookB { FullImport = args.Contains("--full-import") },
                "C" => new PlaybookC { PeerProject = Option("--peer") },
                _ => new PlaybookA(),
            };
            try
            {
                await runner.Execute(session, which + "-r" + run, steps, cancel);
            }
            finally
            {
                stop.Cancel();
                try { await loop; } catch (OperationCanceledException) { }
            }
        }
        return Envelope(verb, true, new { recordings = files });
    }
    case "winprobe":
    {
        // statuslab winprobe [--start] [--full-import] [--recompile] [--slowimport] [--seconds N]
        var target = new Target { ProjectPath = project };
        var seconds = int.Parse(Option("--seconds", "120"));
        var outPath = Path.Combine(resultsRoot, "winprobe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        Directory.CreateDirectory(resultsRoot);
        using var log = new StreamWriter(outPath) { AutoFlush = true };
        void Write(string line) { log.WriteLine(line); Console.Error.WriteLine(line.Length > 400 ? line.Substring(0, 400) : line); }
        Task action = Task.CompletedTask;
        if (args.Contains("--start"))
        {
            if (args.Contains("--full-import") && Directory.Exists(Path.Combine(project, "Library"))) Directory.Delete(Path.Combine(project, "Library"), true);
            action = EditorLauncher.Start(target, cancel);
        }
        else if (!EditorLauncher.TryLoadOwn(target)) return Envelope(verb, false, null, "No own Editor runs on this project.");
        var probe = WindowProbe.Run(() => target.Pid, seconds, Write, cancel);
        if (args.Contains("--recompile") || args.Contains("--slowimport"))
        {
            await Task.Delay(1500, cancel);
            new DescriptorSource().Read(new Snapshot(), target, cancel).Wait();
            if (args.Contains("--slowimport"))
            {
                var folder = Path.Combine(project, "Assets", "StatusLab", "ImportLoad");
                Directory.CreateDirectory(folder);
                for (var i = 0; i < 3; i++) File.WriteAllText(Path.Combine(folder, "probe" + i + ".slowimport"), "probe " + Environment.TickCount + " " + i);
                await PipelineHttp.Exec(target, new[] { "statuslab", "refresh" }, 5000, cancel);
            }
            else
            {
                var touch = Path.Combine(project, "Assets", "StatusLab", "Editor", "Touch.cs");
                File.WriteAllText(touch, System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(touch), @"Value = \d+;", "Value = " + (Environment.TickCount & 0x7fffffff) + ";"));
                await PipelineHttp.Exec(target, new[] { "statuslab", "refresh" }, 5000, cancel);
            }
        }
        await action;
        await probe;
        return Envelope(verb, true, new { output = outPath, pid = target.Pid });
    }
    case "replay":
    {
        var input = args.Length > 1 ? args[1] : resultsRoot;
        var files = Directory.Exists(input) ? Directory.GetFiles(input, "*.ndjson*", SearchOption.AllDirectories).OrderBy(f => f).ToArray() : new[] { input };
        var replayer = new Replayer();
        var overridePath = FindUp("truth-overrides.json");
        if (File.Exists(overridePath))
            foreach (var p in JsonDocument.Parse(File.ReadAllText(overridePath)).RootElement.EnumerateObject())
                if (p.Name != "_reason") replayer.Overrides[p.Name] = p.Value.Deserialize<TruthWindow>(Recorder.Options);
        var reports = files.SelectMany(replayer.Replay).ToList();
        foreach (var r in reports)
            Console.Error.WriteLine($"{(r.Scored ? (r.Pass ? "PASS" : "FAIL") : "----"),-5}{r.Case,-14}{r.Step,-5}{string.Join(" > ", r.Observed),-70} {r.Why}");
        var outFolder = Directory.Exists(input) ? input : Path.GetDirectoryName(Path.GetFullPath(input));
        File.WriteAllText(Path.Combine(outFolder, "summary.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions(Recorder.Options) { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outFolder, "matrix.json"), JsonSerializer.Serialize(replayer.Matrix, new JsonSerializerOptions(Recorder.Options) { WriteIndented = true }));
        var scored = reports.Where(r => r.Scored).ToList();
        return Envelope(verb, scored.All(r => r.Pass), new { cases = scored.Count, pass = scored.Count(r => r.Pass), summary = Path.Combine(outFolder, "summary.json") });
    }
    default:
        return Envelope("help", false, null, "Commands: start, stop, sample, playbook A|B, replay <path>");
}
