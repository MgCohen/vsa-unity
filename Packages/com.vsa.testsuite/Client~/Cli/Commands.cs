using System.Diagnostics;
using VSA.TestSuite.Client.Status;

namespace VSA.TestSuite.Cli
{
    /// <summary>One method for each command. Each method calls the library or the daemon and writes the envelope.</summary>
    static class Commands
    {
        public const string Help = @"vsa-unity: open, close and check one Unity Editor of a project.

  vsa-unity open [--timeout 600] [--observe]
                                         CHANGES THE EDITOR. Starts the daemon of the project. With no Editor:
                                         batch-mode compile check, then a start with -automated. With a running
                                         Editor: attach. Returns when the Editor is ready.
                                         Adopt (default): the daemon answers dialogs, escalates, stops and restarts.
                                         --observe: the daemon only answers dialogs.
  vsa-unity close [--save|--discard]     CHANGES THE EDITOR. Quits it. With unsaved changes and no flag: refuses.
  vsa-unity status                       Reads the state now. Changes nothing.
  vsa-unity wait [--until ready] [--timeout 600]
                                         Waits for a state. Changes nothing.
  vsa-unity log sessions                 Lists the run folders. Changes nothing.
  vsa-unity log show [--run <id>] [--tail 50]
                                         The last lines of unity.log and status.ndjson. Changes nothing.
  vsa-unity log mark ""<text>"" [--run <id>]
                                         Adds a mark line to status.ndjson. Changes no Editor.
  vsa-unity config show                  The values in use and the source of each.

Target:  --project-path <path> (default: the project at or above the current folder)
         --pid <pid> (status and wait with no daemon: read that Editor)
Config:  --log-root / VSA_UNITY_LOG_ROOT, --limits / VSA_UNITY_LIMITS, --keep-runs / VSA_UNITY_KEEP_RUNS
Output:  JSON envelope on stdout (--format human for people). Exit codes: 0, 6, 12, 13, 14, 17, 18.";

        public static async Task<int> Open(Options o, CancellationToken cancel)
        {
            var timeoutS = Timeout(o, 600);
            var project = o.ProjectPath();
            var started = await DaemonClient.Ensure(project, DaemonStart(o, project), cancel);
            if (started) Output.Progress("Started the daemon of " + project + ".");
            var reply = await Send(project, new DaemonRequest { Command = "open", TimeoutMs = timeoutS * 1000L }, cancel);
            return Output.Envelope("open", Data(reply), reply.Code, reply.Message, reply.Warnings);
        }

        public static async Task<int> Close(Options o, CancellationToken cancel)
        {
            if (o.Has("--save") && o.Has("--discard")) throw new ArgumentException("Give --save or --discard, not both.");
            var changes = o.Has("--save") ? "save" : o.Has("--discard") ? "discard" : null;
            var project = o.ProjectPath();
            if (!await DaemonClient.Running(project, cancel))
            {
                if (EditorLauncher.FindUnity(project).Count == 0)
                    return Output.Envelope("close", new { closed = false, reason = "No Editor runs on the project." });
                await DaemonClient.Ensure(project, DaemonStart(o, project), cancel);
            }
            var reply = await Send(project, new DaemonRequest { Command = "close", Changes = changes }, cancel);
            return Output.Envelope("close", Data(reply), reply.Code, reply.Message, reply.Warnings);
        }

        public static async Task<int> Status(Options o, CancellationToken cancel)
        {
            var project = o.ProjectPath();
            if (!o.Has("--pid") && await DaemonClient.Send(project, new DaemonRequest { Command = "status" }, 300, cancel) is DaemonReply reply)
                return Output.Envelope("status", Data(reply), reply.Code, reply.Message, reply.Warnings);
            var target = Target(o, project);
            return Output.Envelope("status", await StatusReader.Read(target, cancel));
        }

        public static async Task<int> Wait(Options o, CancellationToken cancel)
        {
            var until = WaitRules.ParseUntil(o.Get("--until", "ready"));
            var timeoutS = Timeout(o, 600);
            var project = o.ProjectPath();
            if (!o.Has("--pid") && await DaemonClient.Running(project, cancel))
            {
                var reply = await Send(project, new DaemonRequest { Command = "wait", Until = until.ToList(), TimeoutMs = timeoutS * 1000L }, cancel);
                return Output.Envelope("wait", Data(reply), reply.Code, reply.Message, reply.Warnings);
            }
            var target = Target(o, project);
            var waiter = new StatusWaiter(target, Limits(o, project)) { AdoptDescriptorPid = o.Has("--pid") };
            string last = null;
            waiter.Observed = (r, _) =>
            {
                if (r.ToString() == last) return;
                last = r.ToString();
                Output.Progress($"{DateTime.Now:HH:mm:ss} {r}");
            };
            var outcome = await waiter.Wait(until, timeoutS * 1000L, cancel);
            var data = StatusReader.Report(outcome.Result, outcome.Snapshot, target, history: false);
            return Output.Envelope("wait", data, outcome.Code, outcome.Message);
        }

        /// <summary>The daemon entry. "open" and "close" start it as a hidden process. It writes its log to daemon.log.</summary>
        public static async Task<int> Daemon(Options o, CancellationToken cancel)
        {
            var project = o.ProjectPath();
            var target = new Target { ProjectPath = project, LogRoot = LogRoot(o, project) };
            EditorLauncher.KeepRuns = KeepRuns(o, project);
            Directory.CreateDirectory(target.StatusFolder);
            using var log = new StreamWriter(new FileStream(DaemonPaths.Log(target), FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            var daemon = new EditorDaemon(target, adopt: !o.Has("--observe"))
            {
                Log = line => { lock (log) log.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + line); },
            };
            daemon.Log("Daemon PID " + Environment.ProcessId + " for " + project + (o.Has("--observe") ? " (observe)." : " (adopt)."));
            try
            {
                return await daemon.Run(cancel);
            }
            catch (Exception e)
            {
                daemon.Log("Daemon error: " + e);
                return 1;
            }
        }

        public static int Log(Options o, string verb)
        {
            var project = o.ProjectPath();
            var root = LogRoot(o, project);
            switch (verb)
            {
                case "sessions":
                    return Output.Envelope("log sessions", new { root, runs = RunLog.Sessions(root) });
                case "show":
                    return Output.Envelope("log show", RunLog.Show(root, o.Get("--run"), o.Int("--tail", 50)));
                case "mark":
                    var verbs = o.Verbs;
                    if (verbs.Length < 3) throw new ArgumentException("log mark needs the text of the mark.");
                    return Output.Envelope("log mark", new { file = RunLog.Mark(root, o.Get("--run"), verbs[2]), text = verbs[2] });
                default:
                    throw new ArgumentException("Unknown log command '" + verb + "'. Use sessions, show or mark.");
            }
        }

        public static int Config(Options o)
        {
            string project = null;
            try { project = o.ProjectPath(); }
            catch (DirectoryNotFoundException) { }
            var (root, rootSource) = o.Config(project, "--log-root", "VSA_UNITY_LOG_ROOT", "logRoot", project == null ? null : Path.Combine(project, "Logs", "vsa-unity"));
            var (limits, limitsSource) = o.Config(project, "--limits", "VSA_UNITY_LIMITS", "limits", "embedded limits.json");
            var (keep, keepSource) = o.Config(project, "--keep-runs", "VSA_UNITY_KEEP_RUNS", "keepRuns", "20");
            return Output.Envelope("config show", new
            {
                project,
                logRoot = new { value = root, source = rootSource },
                limits = new { value = limits, source = limitsSource },
                keepRuns = new { value = keep, source = keepSource },
            });
        }

        static async Task<DaemonReply> Send(string project, DaemonRequest request, CancellationToken cancel)
            => await DaemonClient.Send(project, request, 5_000, cancel)
                ?? throw new IOException("The daemon of " + project + " did not answer.");

        /// <summary>The process that runs "vsa-unity daemon" for the project, with the options of this call.</summary>
        static ProcessStartInfo DaemonStart(Options o, string project)
        {
            var self = Environment.ProcessPath;
            var args = new List<string>();
            var host = Path.GetFileNameWithoutExtension(self).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
            if (host) args.Add(typeof(Commands).Assembly.Location);
            args.AddRange(new[] { "daemon", "--project-path", project, "--log-root", LogRoot(o, project), "--keep-runs", KeepRuns(o, project).ToString() });
            if (o.Has("--observe")) args.Add("--observe");
            return new ProcessStartInfo(self, string.Join(" ", args.Select(a => "\"" + a + "\""))) { WorkingDirectory = project };
        }

        /// <summary>--pid attaches to that Editor; otherwise the own Editor from own-editor.json, if it runs.</summary>
        static Target Target(Options o, string project)
        {
            var target = new Target { ProjectPath = project, LogRoot = LogRoot(o, project) };
            if (o.Get("--pid") is string text)
            {
                if (!EditorLauncher.TryAttach(target, o.Int("--pid", 0)))
                    throw new ArgumentException("PID " + text + " is not a Unity Editor of " + project + ".");
                return target;
            }
            EditorLauncher.TryLoadOwn(target);
            return target;
        }

        static int Timeout(Options o, int fallback)
        {
            var timeoutS = o.Int("--timeout", fallback);
            if (timeoutS <= 0) throw new ArgumentException("--timeout needs a value above 0.");
            return timeoutS;
        }

        static string LogRoot(Options o, string project)
            => o.Config(project, "--log-root", "VSA_UNITY_LOG_ROOT", "logRoot", Path.Combine(project, "Logs", "vsa-unity")).Value;

        static Limits Limits(Options o, string project)
            => Client.Status.Limits.Load(o.Config(project, "--limits", "VSA_UNITY_LIMITS", "limits", null).Value);

        static int KeepRuns(Options o, string project)
            => int.TryParse(o.Config(project, "--keep-runs", "VSA_UNITY_KEEP_RUNS", "keepRuns", "20").Value, out var n) && n > 0 ? n : 20;

        static object Data(DaemonReply reply) => new
        {
            state = reply.Report?.State,
            result = reply.Report?.Result,
            action = reply.Report?.Action,
            ageMs = reply.Report?.AgeMs,
            sinceProgressMs = reply.Report?.SinceProgressMs,
            history = true,
            phase = reply.Phase,
            mode = reply.Mode,
            attached = reply.Attached,
            daemonPid = reply.DaemonPid,
            pid = reply.Report?.Pid,
            editorId = reply.Report?.EditorId,
            runFolder = reply.Report?.RunFolder,
            dirtyScenes = reply.DirtyScenes,
            errors = reply.Errors,
            exitCode = reply.ExitCode,
            signals = reply.Report?.Signals,
        };
    }
}
