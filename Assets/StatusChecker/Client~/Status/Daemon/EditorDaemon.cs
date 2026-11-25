using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace VSA.StatusChecker
{
    /// <summary>
    /// The background process of one project. It finds the Editor (own, attached, or started after the batch-mode
    /// compile check), samples it, records each sample to status.ndjson, lets the supervisor act, and answers
    /// clients on a named pipe. It ends when the Editor exits, after a close, or after a failed start.
    /// </summary>
    public sealed class EditorDaemon
    {
        /// <summary>The daemon ends when the Editor process is gone this long and no restart runs.</summary>
        const long EndAfterGoneMs = 60_000;

        /// <summary>The time with a good probe and no fresh beacon before the daemon turns the beacon on.</summary>
        const long BeaconGraceMs = 3_000;

        /// <summary>The time between two tries to turn the beacon on.</summary>
        const long BeaconRetryMs = 60_000;

        readonly Target target;
        readonly IEditorControl control;
        readonly bool adopt;
        readonly List<string> warnings = new();
        readonly List<Task> clients = new();
        readonly CancellationTokenSource end = new();
        readonly Stopwatch clock = Stopwatch.StartNew();
        LiveSession session;
        DaemonReply fatal;
        volatile string phase = "starting";
        bool attached;
        bool closing;
        long beaconMissingSince = -1;
        long beaconAskedAt = -1;

        /// <summary>Makes the daemon of a project. adopt true: the supervisor can escalate, stop and restart the Editor.
        /// adopt false (observe): it only answers dialogs.</summary>
        public EditorDaemon(Target target, bool adopt, IEditorControl control = null)
        {
            this.target = target;
            this.adopt = adopt;
            this.control = control ?? new EditorControl();
        }

        /// <summary>Called with each line of the daemon log.</summary>
        public Action<string> Log { get; set; } = _ => { };

        /// <summary>Runs the daemon. Returns 1 if another daemon holds the lock of the project or the start failed.</summary>
        public async Task<int> Run(CancellationToken cancel = default)
        {
            Directory.CreateDirectory(target.StatusFolder);
            FileStream lockFile;
            try
            {
                lockFile = new FileStream(DaemonPaths.Lock(target), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                Log("Another daemon holds the lock of " + target.ProjectPath + ".");
                return 1;
            }
            using (lockFile)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, end.Token);
                var serve = Serve(linked.Token, cancel);
                try
                {
                    await Acquire(linked.Token);
                    if (fatal == null) await Supervise(linked.Token);
                    else await Task.Delay(30_000, linked.Token);
                }
                catch (OperationCanceledException) { }
                finally
                {
                    if (phase != "failed") phase = "ended";
                    end.Cancel();
                    try { await serve; }
                    catch (OperationCanceledException) { }
                    Task[] open;
                    lock (clients) open = clients.ToArray();
                    await Task.WhenAny(Task.WhenAll(open), Task.Delay(3_000));
                    Log("The daemon ends: " + phase + ".");
                }
            }
            return fatal == null ? 0 : 1;
        }

        async Task Acquire(CancellationToken cancel)
        {
            if (EditorLauncher.TryLoadOwn(target))
            {
                Log("Own Editor, PID " + target.Pid + ".");
                return;
            }
            var running = EditorLauncher.FindUnity(target.ProjectPath);
            if (running.Count > 0)
            {
                var row = running[0];
                if (!EditorLauncher.TryAttach(target, row.Pid))
                {
                    Fail("AttachFailed", "PID " + row.Pid + " runs on the project, but the daemon cannot open it.", null);
                    return;
                }
                attached = true;
                if (row.CommandLine.IndexOf("-automated", StringComparison.OrdinalIgnoreCase) < 0) warnings.Add("not-automated");
                target.EditorId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-a" + Guid.NewGuid().ToString("N").Substring(0, 5);
                Directory.CreateDirectory(target.RunFolder);
                File.WriteAllText(Path.Combine(target.RunFolder, RunLog.RunFile), JsonSerializer.Serialize(new
                {
                    pid = target.Pid,
                    logPath = target.LogPath,
                    editorId = target.EditorId,
                    attached = true,
                }));
                Log("Attached to PID " + target.Pid + (warnings.Contains("not-automated") ? ", started without -automated." : "."));
                return;
            }
            try
            {
                Log("No Editor runs: compile check, then start.");
                await control.Start(target, cancel);
                Log("Started PID " + target.Pid + ", log " + target.LogPath + ".");
            }
            catch (StartRefusedException e)
            {
                Fail("StartRefused", e.Message + (e.LogPath == null ? "" : " Log: " + e.LogPath), e.Errors.ToList());
            }
            catch (TimeoutException e)
            {
                Fail("StartFailed", e.Message, null);
            }
        }

        async Task Supervise(CancellationToken cancel)
        {
            session = new LiveSession(target, Path.Combine(target.RunFolder, RunLog.StatusFile), control);
            if (attached) session.Actions.Add("daemon", "attach");
            session.Supervisor.Mode = adopt ? SupervisorMode.Live : SupervisorMode.Observe;
            session.Supervisor.RestartOnCrash = adopt;
            session.Actions.Added += a => Log("action " + a.Who + " " + a.Kind + (a.Detail == null ? "" : ": " + a.Detail));
            phase = "running";
            var loop = session.Run(cancel);
            long goneSince = -1;
            string last = null;
            while (!cancel.IsCancellationRequested)
            {
                await Task.WhenAny(loop, Task.Delay(250, cancel));
                if (loop.IsCompleted)
                {
                    await loop;
                    return;
                }
                var s = session.LastSnapshot;
                var r = session.Last;
                if (s == null || closing) continue;
                if (r.ToString() != last) Log("state " + (last = r.ToString()));
                // An exit after an escalation step (quit, close, stop) is not the end: the supervisor starts again.
                if (r.State == "exited" && !Restarting && !session.Tracker.Memory.StopSent) return;
                if (s.ProcAlive) goneSince = -1;
                else if (!Restarting)
                {
                    if (goneSince < 0) goneSince = clock.ElapsedMilliseconds;
                    if (session.Supervisor.Failed || clock.ElapsedMilliseconds - goneSince > EndAfterGoneMs) return;
                }
                await CheckBeacon(s, cancel);
            }
        }

        /// <summary>True while the supervisor runs a recovery start.</summary>
        bool Restarting => session.Actions.Entries.LastOrDefault(e => e.Kind is "recovery" or "own-pid" or "fail-run")?.Kind == "recovery";

        /// <summary>If the probe answers but the beacon is not fresh, turn the beacon on (once in each minute).
        /// If the package is missing, keep the native signals and warn "no-beacon".</summary>
        async Task CheckBeacon(Snapshot s, CancellationToken cancel)
        {
            if (target.ProbeMissing && !warnings.Contains("no-beacon"))
            {
                warnings.Add("no-beacon");
                Log("The Editor has no suite ping: the package is missing. Native signals only.");
            }
            if (!s.ProbeOk || s.BeaconFresh)
            {
                beaconMissingSince = -1;
                return;
            }
            var now = clock.ElapsedMilliseconds;
            if (beaconMissingSince < 0) beaconMissingSince = now;
            if (now - beaconMissingSince < BeaconGraceMs) return;
            if (beaconAskedAt >= 0 && now - beaconAskedAt < BeaconRetryMs) return;
            beaconAskedAt = now;
            if (target.ProbeMissing) return;
            var (http, body, _) = await PipelineHttp.Exec(target, Target.BeaconOn, 2000, cancel);
            var on = http == 200 && JsonFind.Bool(JsonFind.Parse(body), "beacon") == true;
            Log("Beacon off: turned it on (" + (on ? "ok" : "HTTP " + http) + ").");
            if (!on && !warnings.Contains("no-beacon")) warnings.Add("no-beacon");
        }

        async Task Serve(CancellationToken accept, CancellationToken process)
        {
            var name = DaemonPaths.PipeName(target.ProjectPath);
            while (!accept.IsCancellationRequested)
            {
                var server = new NamedPipeServerStream(name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                try
                {
                    await server.WaitForConnectionAsync(accept);
                }
                catch (OperationCanceledException)
                {
                    server.Dispose();
                    return;
                }
                var task = Task.Run(() => Client(server, process));
                lock (clients)
                {
                    clients.RemoveAll(t => t.IsCompleted);
                    clients.Add(task);
                }
            }
        }

        async Task Client(NamedPipeServerStream server, CancellationToken cancel)
        {
            using (server)
            {
                try
                {
                    using var reader = new StreamReader(server, leaveOpen: true);
                    using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };
                    var line = await reader.ReadLineAsync(cancel);
                    if (line == null) return;
                    var request = JsonSerializer.Deserialize<DaemonRequest>(line, Recorder.Options);
                    var reply = await Handle(request, cancel);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(reply, Recorder.Options).AsMemory(), cancel);
                }
                catch (Exception e) when (e is IOException or OperationCanceledException or JsonException)
                {
                    Log("client: " + e.GetType().Name + " " + e.Message);
                }
            }
        }

        /// <summary>Answers one request.</summary>
        public async Task<DaemonReply> Handle(DaemonRequest request, CancellationToken cancel)
        {
            switch (request?.Command)
            {
                case "status":
                    return Reply(null, null);
                case "open":
                    return await Wait(new HashSet<string>(WaitRules.Ready), request.TimeoutMs, cancel);
                case "wait":
                    return await Wait(new HashSet<string>(request.Until ?? WaitRules.Ready.ToList()), request.TimeoutMs, cancel);
                case "close":
                    return await Close(request.Changes, cancel);
                default:
                    return Reply("BadArguments", "Unknown request '" + request?.Command + "'.");
            }
        }

        async Task<DaemonReply> Wait(ISet<string> until, long timeoutMs, CancellationToken cancel)
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                if (fatal != null)
                {
                    end.CancelAfter(1_000);
                    return fatal;
                }
                if (phase == "ended") return Reply("NoEditor", "The Editor exited and the daemon ended.");
                var r = session?.Last;
                if (r != null && !closing)
                {
                    if (until.Contains(r.State) && r.Result == null) return Reply(null, null);
                    var code = WaitRules.DaemonStopCode(r, adopt, session.Supervisor.Failed);
                    if (code == "NoEditor" && (Restarting || session.Tracker.Memory.StopSent) && adopt) code = null;
                    if (code != null) return Reply(code, r + (r.Evidence == null ? "" : ": " + r.Evidence));
                }
                if (timeoutMs > 0 && watch.ElapsedMilliseconds >= timeoutMs)
                    return Reply("Timeout", "The state is " + (r?.ToString() ?? phase) + " after " + timeoutMs / 1000 + " s.");
                await Task.Delay(250, cancel);
            }
        }

        async Task<DaemonReply> Close(string changes, CancellationToken cancel)
        {
            if (session == null) return Reply("NoEditor", "The daemon has no Editor.");
            if (closing) return Reply("Busy", "A close runs.");
            var s = session.LastSnapshot;
            if (changes == null)
            {
                if (s?.ProbeOk != true)
                    return Reply("UnknownChanges", "The main thread gives no answer, so the unsaved changes are not known. Pass --save or --discard.");
                if (s.ProbeDirtyScenes > 0)
                {
                    var dirty = Reply("UnsavedChanges", s.ProbeDirtyScenes + " scene(s) have unsaved changes. Pass --save or --discard.");
                    dirty.DirtyScenes = (s.ProbeDirtyScenePaths ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
                    return dirty;
                }
            }
            closing = true;
            session.Supervisor.Mode = SupervisorMode.Off;
            session.Actions.Add("daemon", "stop", "close " + (changes ?? "clean"));
            var process = target.Process;
            if (s != null && s.ServerUp(session.Tracker.Memory))
            {
                var (exit, output) = await EditorControl.QuitWith(target, changes == null ? null : "--" + changes, cancel);
                Log("editor quit: exit " + exit + " " + output);
            }
            if (!await Exited(process, 20_000))
            {
                session.Actions.Add("daemon", "close-window");
                control.CloseWindow(target);
                if (!await Exited(process, 15_000))
                {
                    session.Actions.Add("daemon", "stop-pid");
                    control.Stop(target);
                    await Exited(process, 10_000);
                }
            }
            var reply = Reply(process == null || process.HasExited ? null : "CloseFailed", process == null || process.HasExited ? "The Editor exited." : "The Editor still runs.");
            reply.ExitCode = process is { HasExited: true } ? process.ExitCode : null;
            if (reply.Code == null)
            {
                phase = "ended";
                reply.Phase = phase;
                end.CancelAfter(500);
            }
            else
            {
                closing = false;
                session.Supervisor.Mode = adopt ? SupervisorMode.Live : SupervisorMode.Observe;
            }
            return reply;
        }

        static async Task<bool> Exited(Process process, int timeoutMs)
        {
            if (process == null) return true;
            using var timeout = new CancellationTokenSource(timeoutMs);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return process.HasExited;
            }
        }

        void Fail(string code, string message, List<string> errors)
        {
            phase = "failed";
            fatal = Reply(code, message);
            fatal.Errors = errors;
            Log("Failed: " + code + " " + message);
        }

        DaemonReply Reply(string code, string message) => new()
        {
            Code = code,
            Message = message,
            Phase = phase,
            Mode = adopt ? "adopt" : "observe",
            Attached = attached,
            DaemonPid = Environment.ProcessId,
            Report = StatusReader.Report(session?.Last, session?.LastSnapshot, target, history: true),
            Warnings = warnings.ToList(),
        };
    }
}
