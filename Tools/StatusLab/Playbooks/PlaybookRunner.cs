using VSA.TestSuite.Client.Status;
using System.Diagnostics;

namespace StatusLab
{
    /// <summary>Shared steps of the playbooks: run a test case with a truth window, and the Editor helpers.</summary>
    public abstract class PlaybookRunner
    {
        protected LiveSession S;
        protected CancellationToken Cancel;
        protected Target T => S.Target;
        protected string Project => S.Target.ProjectPath;
        protected string Run;

        public async Task Execute(LiveSession session, string run, ISet<string> only, CancellationToken cancel)
        {
            S = session;
            Run = run;
            Cancel = cancel;
            await Steps(only);
        }

        protected abstract Task Steps(ISet<string> only);

        protected static bool Want(ISet<string> only, string step) => only == null || only.Count == 0 || only.Contains(step);

        /// <summary>Runs one test case: trigger, wait for the end or the window, then writes the truth window.</summary>
        protected async Task Case(string id, string step, string[] allowed, int windowMs, int detectMs, Func<Task> trigger,
            Func<StateResult, bool> endWhen = null, string expect = null, bool scored = true, SupervisorMode mode = SupervisorMode.Off, string key = null)
        {
            S.Actions.Add("playbook", "recovery-reset");
            S.Supervisor.Reset();
            S.Supervisor.Mode = mode;
            var t0 = S.Clock.NowMs;
            S.Recorder.Meta("case " + id + " " + step + " start", t0);
            Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss} {Run} {id} {step} start");
            if (trigger != null) await trigger();
            var until = t0 + windowMs;
            while (S.Clock.NowMs < until)
            {
                if (endWhen != null && S.Last != null && endWhen(S.Last)) break;
                if (S.Supervisor.Failed) break;
                await Task.Delay(250, Cancel);
            }
            if (endWhen != null) await Task.Delay(1500, Cancel);   // a short tail after the end
            S.Supervisor.Mode = SupervisorMode.Off;
            var w = new TruthWindow
            {
                Case = Run + "/" + id,
                Step = step,
                T0 = t0,
                T1 = S.Clock.NowMs,
                Allowed = allowed.ToList(),
                DetectMs = detectMs,
                ExpectResult = expect,
                Scored = scored,
                Key = key,
            };
            S.Recorder.Truth(w);
            Console.Error.WriteLine($"{DateTime.Now:HH:mm:ss} {Run} {id} end, last {S.Last}");
        }

        /// <summary>An end condition that is true after the states appeared in this order.</summary>
        protected static Func<StateResult, bool> After(params string[] sequence)
        {
            var i = 0;
            return r =>
            {
                if (i < sequence.Length && sequence[i].Split('|').Contains(r.State)) i++;
                return i >= sequence.Length;
            };
        }

        protected async Task<(int Http, string Body, double Ms)> Exec(int timeoutMs, params string[] argv)
        {
            var r = await PipelineHttp.Exec(T, argv, timeoutMs, Cancel);
            var body = r.Body ?? "";
            S.Recorder.Meta("exec " + string.Join(" ", argv) + " -> " + r.Http + " " + (body.Length > 300 ? body.Substring(0, 300) : body), S.Clock.NowMs);
            return r;
        }

        /// <summary>Runs "unity &lt;args&gt;" in the project folder and returns the exit code.</summary>
        protected async Task<int> Cli(int timeoutS, params string[] args)
        {
            var info = new ProcessStartInfo("unity")
            {
                WorkingDirectory = Project,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var a in args) info.ArgumentList.Add(a);
            info.ArgumentList.Add("--no-banner");
            using var p = Process.Start(info);
            var output = p.StandardOutput.ReadToEndAsync();
            _ = p.StandardError.ReadToEndAsync();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Cancel);
            timeout.CancelAfter(timeoutS * 1000);
            try { await p.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) when (!Cancel.IsCancellationRequested) { return -1; }
            var text = await output;
            S.Recorder.Meta("cli " + string.Join(" ", args) + " -> " + p.ExitCode + " " + (text.Length > 600 ? text.Substring(0, 600) : text), S.Clock.NowMs);
            return p.ExitCode;
        }

        /// <summary>Starts the own Editor if none runs, and waits for ready.</summary>
        protected async Task EnsureEditor(int readyTimeoutMs = 600_000)
        {
            if (T.Process is { HasExited: false } && S.Last?.State == "ready-idle") return;
            if (T.Process is not { HasExited: false }) await StartEditor();
            await S.WaitFor(r => r.State == "ready-idle", readyTimeoutMs, Cancel);
        }

        protected async Task StartEditor()
        {
            S.Actions.Add("playbook", "forget");
            await S.Control.Start(T, Cancel);
            S.Actions.Add("playbook", "own-pid", T.Pid.ToString());
        }

        /// <summary>Quits the own Editor with "editor quit --discard", then stops it by PID if it still runs after 30 s.</summary>
        protected async Task QuitEditor()
        {
            if (T.Process is not { HasExited: false } p) return;
            await S.Control.Quit(T, Cancel);
            var until = DateTime.UtcNow.AddSeconds(30);
            while (!p.HasExited && DateTime.UtcNow < until) await Task.Delay(500, Cancel);
            if (!p.HasExited) S.Control.Stop(T);
            await Task.Delay(1500, Cancel);
        }

        protected void Forget() => S.Actions.Add("playbook", "forget");

        /// <summary>Waits for the own process to exit and records the time from now to the exit.</summary>
        protected async Task WaitExit(int timeoutMs)
        {
            var start = S.Clock.NowMs;
            while (T.Process is { HasExited: false } && S.Clock.NowMs - start < timeoutMs) await Task.Delay(250, Cancel);
            var exited = T.Process is not { HasExited: false };
            S.Recorder.Meta("exit wait " + (S.Clock.NowMs - start) + " ms, exited " + exited + (exited && T.Process != null ? ", code " + T.Process.ExitCode : ""), S.Clock.NowMs);
            if (!exited) S.Control.Stop(T);
        }

        protected string AssetPath(params string[] parts) => Path.Combine(new[] { Project, "Assets", "StatusLab" }.Concat(parts).ToArray());

        /// <summary>Library/StatusLab of the project: the folder of hang.flag, read by HangOnLoad in Assets/StatusLab.</summary>
        protected string LabFolder => Path.Combine(Project, "Library", "StatusLab");

        protected string HangFlag => Path.Combine(LabFolder, "hang.flag");

        protected void WriteHangFlag()
        {
            Directory.CreateDirectory(LabFolder);
            File.WriteAllText(HangFlag, "hang");
        }

        protected void DeleteHangFlag()
        {
            if (File.Exists(HangFlag)) File.Delete(HangFlag);
        }

        /// <summary>Changes the constant in Touch.cs, so the next recompile compiles and reloads.</summary>
        protected void TouchCode(int value)
        {
            var path = AssetPath("Editor", "Touch.cs");
            var text = File.ReadAllText(path);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"Value = \d+;", "Value = " + value + ";");
            File.WriteAllText(path, text);
        }

        protected void WriteBroken()
        {
            var folder = AssetPath("Broken");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "StatusLab.Broken.asmdef"), "{ \"name\": \"StatusLab.Broken\" }\n");
            File.WriteAllText(Path.Combine(folder, "Broken.cs"), "// Written by the status lab. A compile error on purpose.\nclass StatusLabBroken { int value = ; }\n");
        }

        protected void DeleteFolder(string folder)
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            if (File.Exists(folder + ".meta")) File.Delete(folder + ".meta");
        }

        /// <summary>Starts a background "unity recompile". It does not wait for the end.</summary>
        protected void Recompile() => _ = Cli(300, "recompile");
    }
}
