using System.Diagnostics;

namespace StatusLab
{
    /// <summary>Playbook C: the added cases. Server down, ports, foreign descriptor, two Editors on one project,
    /// full queue, hang in game code, package error at start. Steps C1-C7, test cases T30-T37.</summary>
    public sealed class PlaybookC : PlaybookRunner
    {
        /// <summary>A second project with its own Editor, for the foreign descriptor case.</summary>
        public string PeerProject { get; set; }

        protected override async Task Steps(ISet<string> only)
        {
            if (Want(only, "C1"))
            {
                await EnsureEditor();
                await Case("T30", "C1", new[] { "ready-idle", "long-command", "server-down|domain-reload", "cold-start|domain-reload|settling", "ready-idle" }, 90_000, 15_000,
                    async () => await Exec(5000, "statuslab", "server_off", "--seconds", "40"),
                    After("server-down", "ready-idle"), mode: SupervisorMode.DryRun, key: "server-down");
            }

            if (Want(only, "C2"))
            {
                // (a) The port of the last start is taken before the start: the Editor must take another one.
                await EnsureEditor();
                var port = T.Port;
                await QuitEditor();
                Forget();
                using (var blocker = PortBlocker.Take(port, port))
                {
                    S.Recorder.Meta("blocked ports " + string.Join(",", blocker.Ports), S.Clock.NowMs);
                    await Case("T31", "C2", new[] { "no-editor", "cold-start", "settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                        600_000, 15_000, StartEditor, After("ready-idle"));
                    S.Recorder.Meta("descriptor port " + T.Port + " (blocked " + port + ")", S.Clock.NowMs);
                }
                // (b) All free ports of the Editor range are taken: the HTTP server cannot start.
                await QuitEditor();
                Forget();
                using (var blocker = PortBlocker.Take(7800, 7849))
                {
                    S.Recorder.Meta("blocked ports " + blocker.Ports.Count, S.Clock.NowMs);
                    await Case("T32", "C2", new[] { "no-editor", "cold-start", "server-down" }, 200_000, 15_000, StartEditor,
                        r => r.Result is "stalled" or "stuck", expect: "stalled|stuck", mode: SupervisorMode.DryRun);
                }
                await Case("T32c", "C2", new[] { "cold-start", "settling|importing|compiling|domain-reload|long-command|server-down", "ready-idle" }, 120_000, 0,
                    null, After("ready-idle"), scored: false);
                if (S.Last?.State != "ready-idle")
                {
                    await QuitEditor();
                    Forget();
                    await EnsureEditor();
                }
            }

            if (Want(only, "C3") && PeerProject != null)
            {
                // A descriptor of another live Editor in this project's Library.
                await EnsureEditor();
                var peer = new Target { ProjectPath = PeerProject };
                await EditorLauncher.Start(peer, Cancel);
                S.Recorder.Meta("peer pid " + peer.Pid, S.Clock.NowMs);
                var until = DateTime.UtcNow.AddMinutes(10);
                while (!File.Exists(peer.DescriptorPath) && DateTime.UtcNow < until) await Task.Delay(1000, Cancel);
                try
                {
                    await Case("T33", "C3", new[] { "ready-idle", "descriptor-mismatch", "ready-idle" }, 60_000, 5_000,
                        () => { File.Copy(peer.DescriptorPath, T.DescriptorPath, true); return Task.CompletedTask; },
                        After("descriptor-mismatch", "ready-idle"), key: "descriptor-mismatch");
                }
                finally
                {
                    var control = new EditorControl();
                    control.CloseWindow(peer);
                    if (peer.Process != null && !peer.Process.WaitForExit(30_000)) control.Stop(peer);
                }
            }

            if (Want(only, "C4"))
            {
                // A second Unity.exe on the same project, started directly (the launcher refuses it).
                await EnsureEditor();
                Process second = null;
                await Case("T34", "C4", new[] { "ready-idle" }, 60_000, 0, () =>
                {
                    var version = File.ReadLines(Path.Combine(Project, "ProjectSettings", "ProjectVersion.txt")).First(l => l.StartsWith("m_EditorVersion:")).Split(':')[1].Trim();
                    var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor", version, "Editor", "Unity.exe");
                    var log = Path.Combine(Project, "Logs", "vsa-watch", "second-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), "unity.log");
                    Directory.CreateDirectory(Path.GetDirectoryName(log));
                    var info = new ProcessStartInfo(exe) { UseShellExecute = false };
                    foreach (var a in new[] { "-projectPath", Project, "-automated", "-logFile", log }) info.ArgumentList.Add(a);
                    second = Process.Start(info);
                    S.Recorder.Meta("second pid " + second.Id + " log " + log, S.Clock.NowMs);
                    return Task.CompletedTask;
                });
                if (second != null)
                {
                    second.Refresh();
                    var windows = second.HasExited ? "" : string.Join(" | ", Win32.TopWindows(second.Id).Select(h => Win32.ClassName(h) + ":" + Win32.Text(h)));
                    S.Recorder.Meta("second exited " + second.HasExited + (second.HasExited ? " code " + second.ExitCode : " windows " + windows), S.Clock.NowMs);
                    if (!second.HasExited) second.Kill();
                }
            }

            if (Want(only, "C5"))
            {
                // More than 100 waiting requests: a 20 s hold, then 110 pings at the same time.
                await EnsureEditor();
                var codes = new System.Collections.Concurrent.ConcurrentBag<int>();
                await Case("T35", "C5", new[] { "ready-idle", "long-command|queue-full", "ready-idle" }, 120_000, 10_000, () =>
                {
                    S.Actions.Add("playbook", "job-start", "statuslab hold 20");
                    _ = Task.Run(async () =>
                    {
                        await PipelineHttp.Exec(T, new[] { "statuslab", "hold", "--seconds", "20" }, 30_000, Cancel);
                        S.Actions.Add("playbook", "job-end", "statuslab hold 20");
                    });
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000, Cancel);
                        var pings = Enumerable.Range(0, 110).Select(async _ => codes.Add((await PipelineHttp.Exec(T, new[] { "statuslab", "ping" }, 60_000, Cancel)).Http));
                        await Task.WhenAll(pings);
                        S.Recorder.Meta("ping codes " + string.Join(", ", codes.GroupBy(c => c).Select(g => g.Key + " x" + g.Count())), S.Clock.NowMs);
                    });
                    return Task.CompletedTask;
                }, After("long-command|queue-full", "ready-idle"), key: "long-command");
            }

            if (Want(only, "C6"))
            {
                // A hang in game code: Update blocks the main thread in Play Mode.
                await EnsureEditor();
                S.Actions.Add("playbook", "play-command", "start");
                await Cli(60, "command", "editor.playmode", "start", "--pid", T.Pid.ToString());
                await S.WaitFor(r => r.State == "play-running", 60_000, Cancel);
                await Case("T36", "C6", new[] { "play-running", "long-command", "frozen", "play-running" }, 110_000, 5_000,
                    async () => await Exec(5000, "statuslab", "play_hang", "--seconds", "90"),
                    After("frozen", "play-running"), expect: "stalled", mode: SupervisorMode.DryRun, key: "frozen");
                S.Actions.Add("playbook", "play-command", "stop");
                await Cli(60, "command", "editor.playmode", "stop", "--pid", T.Pid.ToString());
                await S.WaitFor(r => r.State == "ready-idle", 60_000, Cancel);
            }

            if (Want(only, "C7"))
            {
                // A package that cannot resolve, at start. The manifest is restored in all cases.
                await QuitEditor();
                Forget();
                var manifest = Path.Combine(Project, "Packages", "manifest.json");
                var lockFile = Path.Combine(Project, "Packages", "packages-lock.json");
                var original = File.ReadAllText(manifest);
                var originalLock = File.Exists(lockFile) ? File.ReadAllText(lockFile) : null;
                try
                {
                    File.WriteAllText(manifest, original.Replace("\"dependencies\": {", "\"dependencies\": {\n    \"com.statuslab.missing\": \"9.9.9\","));
                    var check = await EditorLauncher.Precheck(T, Cancel);
                    S.Recorder.Meta("precheck with bad package: exit " + check.ExitCode + " in " + check.Ms + " ms", S.Clock.NowMs);
                    await Case("T37", "C7", new[] { "no-editor", "cold-start", "start-failed" },
                        120_000, 20_000, StartEditor, After("start-failed"), key: "start-failed");
                    S.Recorder.Meta("bad package state " + S.Last + " dialog " + S.LastSnapshot?.DialogTitle + " [" + S.LastSnapshot?.DialogButtons + "] main " + S.LastSnapshot?.MainTitle, S.Clock.NowMs);
                }
                finally
                {
                    await QuitEditor();
                    if (T.Process is { HasExited: false }) S.Control.Stop(T);
                    File.WriteAllText(manifest, original);
                    if (originalLock != null) File.WriteAllText(lockFile, originalLock);
                    Forget();
                }
                await Case("T37c", "C7", new[] { "no-editor", "cold-start", "settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                    600_000, 0, StartEditor, After("ready-idle"), scored: false);
            }
        }
    }
}
