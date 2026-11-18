using VSA.TestSuite.Client.Status;

namespace VSA.TestSuite.Cli
{
    /// <summary>One method for each command. Each method calls one library method and writes the envelope.</summary>
    static class Commands
    {
        public const string Help = @"vsa-unity: status checker and supervisor of one Unity Editor.

  vsa-unity status                       Reads the state now. Changes nothing.
  vsa-unity wait [--until ready] [--timeout 600]
                                         Waits for a state. Changes nothing.
  vsa-unity watch [--timeout 3600] [--dry-run]
                                         CHANGES THE EDITOR: starts it (after a batch-mode compile check),
                                         answers dialogs, escalates stalls, restarts one time. NDJSON stream.
  vsa-unity log sessions                 Lists the run folders. Changes nothing.
  vsa-unity log show [--run <id>] [--tail 50]
                                         The last lines of unity.log and watch.ndjson. Changes nothing.
  vsa-unity log mark ""<text>"" [--run <id>]
                                         Adds a mark line to watch.ndjson. Changes no Editor.
  vsa-unity config show                  The values in use and the source of each.

Target:  --project-path <path> (default: the project at or above the current folder)
         --pid <pid> (status and wait only: read an Editor that this tool did not start)
Config:  --log-root / VSA_UNITY_LOG_ROOT, --limits / VSA_UNITY_LIMITS, --keep-runs / VSA_UNITY_KEEP_RUNS
Output:  JSON envelope on stdout (--format human for people). Exit codes: 0, 6, 12, 13, 14, 18.";

        public static async Task<int> Status(Options o, CancellationToken cancel)
        {
            var target = Target(o, allowPid: true);
            var report = await StatusReader.Read(target, cancel);
            return Output.Envelope("status", report);
        }

        public static async Task<int> Wait(Options o, CancellationToken cancel)
        {
            var until = WaitRules.ParseUntil(o.Get("--until", "ready"));
            var timeoutS = o.Int("--timeout", 600);
            if (timeoutS <= 0) throw new ArgumentException("--timeout needs a value above 0.");
            var target = Target(o, allowPid: true);
            var waiter = new StatusWaiter(target, Limits(o, target.ProjectPath)) { AdoptDescriptorPid = o.Has("--pid") };
            string last = null;
            waiter.Observed = (r, _) =>
            {
                if (r.ToString() == last) return;
                last = r.ToString();
                Output.Progress($"{DateTime.Now:HH:mm:ss} {r}");
            };
            WaitOutcome outcome;
            try
            {
                outcome = await waiter.Wait(until, timeoutS * 1000L, cancel);
            }
            catch (OperationCanceledException)
            {
                outcome = new WaitOutcome { Code = "Cancelled", Message = "The wait was cancelled.", Result = waiter.Last, Snapshot = waiter.LastSnapshot };
            }
            return Output.Envelope("wait", WaitData(outcome, target, until), outcome.Code, outcome.Message);
        }

        public static async Task<int> Watch(Options o, CancellationToken cancel)
        {
            if (o.Has("--pid")) throw new ArgumentException("watch acts only on an Editor that it starts. --pid is not permitted.");
            var timeoutS = o.Int("--timeout", 3600);
            if (timeoutS <= 0) throw new ArgumentException("--timeout needs a value above 0.");
            var target = Target(o, allowPid: false);
            EditorLauncher.KeepRuns = KeepRuns(o, target.ProjectPath);
            var run = new WatchRun(target)
            {
                Mode = o.Has("--dry-run") ? SupervisorMode.DryRun : SupervisorMode.Live,
                Line = Output.Line,
            };
            var outcome = await run.Run(timeoutS * 1000L, cancel);
            return Output.Envelope("watch", WaitData(outcome, target, null), outcome.Code, outcome.Message);
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

        /// <summary>The target: --pid attaches to that Editor; otherwise the own Editor from own-editor.json, if it runs.</summary>
        static Target Target(Options o, bool allowPid)
        {
            var project = o.ProjectPath();
            var target = new Target { ProjectPath = project, LogRoot = LogRoot(o, project) };
            if (allowPid && o.Get("--pid") is string text)
            {
                var pid = o.Int("--pid", 0);
                if (!EditorLauncher.TryAttach(target, pid))
                    throw new ArgumentException("PID " + text + " is not a Unity Editor of " + project + ".");
                return target;
            }
            EditorLauncher.TryLoadOwn(target);
            return target;
        }

        static string LogRoot(Options o, string project)
            => o.Config(project, "--log-root", "VSA_UNITY_LOG_ROOT", "logRoot", Path.Combine(project, "Logs", "vsa-unity")).Value;

        static Limits Limits(Options o, string project)
        {
            var path = o.Config(project, "--limits", "VSA_UNITY_LIMITS", "limits", null).Value;
            return Client.Status.Limits.Load(path);
        }

        static int KeepRuns(Options o, string project)
            => int.TryParse(o.Config(project, "--keep-runs", "VSA_UNITY_KEEP_RUNS", "keepRuns", "20").Value, out var n) && n > 0 ? n : 20;

        static object WaitData(WaitOutcome outcome, Target target, ISet<string> until) => new
        {
            state = outcome.Result?.State,
            result = outcome.Result?.Result,
            action = outcome.Result?.Action,
            ageMs = outcome.Result?.AgeMs,
            sinceProgressMs = outcome.Result?.SinceProgressMs,
            elapsedMs = outcome.ElapsedMs,
            until,
            pid = target.Pid,
            editorId = target.EditorId,
            runFolder = target.RunFolder,
            signals = outcome.Snapshot,
        };
    }
}
