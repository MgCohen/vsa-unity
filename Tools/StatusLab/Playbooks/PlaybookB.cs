using System.Diagnostics;

namespace StatusLab
{
    /// <summary>Playbook B: the Editor stops and starts. Steps B1-B8, test cases T17-T25.</summary>
    public sealed class PlaybookB : PlaybookRunner
    {
        public bool FullImport { get; set; }

        protected override async Task Steps(ISet<string> only)
        {
            if (Want(only, "B1"))
            {
                await QuitEditor();
                Forget();
                await Case("T17", "B1", new[] { "no-editor" }, 10_000, 2_000, null);
            }

            if (Want(only, "B2"))
            {
                await QuitEditor();
                Forget();
                if (FullImport) DeleteLibrary();
                await Case(FullImport ? "T19" : "T18", "B2", new[] { "no-editor", "cold-start", "settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                    FullImport ? 3_600_000 : 600_000, 15_000, async () =>
                    {
                        if (FullImport) S.Actions.Add("playbook", "full-import");
                        await StartEditor();
                    }, After("ready-idle"));
            }

            if (Want(only, "B3"))
            {
                await EnsureEditor();
                await Case("T20", "B3", new[] { "ready-idle", "long-command|domain-reload", "exited" }, 45_000, 15_000,
                    async () => await S.Control.Quit(T, Cancel), After("exited"));
                Forget();
            }

            if (Want(only, "B4"))
            {
                await EnsureEditor();
                await Case("T21", "B4", new[] { "ready-idle", "crash" }, 30_000, 5_000,
                    async () => await Exec(5000, "statuslab", "crash"), After("crash"));
                await WaitExit(120_000);
                S.Recorder.Meta("bug reporter processes: " + Process.GetProcessesByName("UnityBugReporter").Length, S.Clock.NowMs);
                Forget();
                await Case("T21b", "B4", new[] { "stale-descriptor", "no-editor" }, 8_000, 0, null);
            }

            if (Want(only, "B5"))
            {
                await EnsureEditor();
                await Case("T22", "B5", new[] { "ready-idle", "crash" }, 15_000, 3_000,
                    () => { S.Control.Stop(T); return Task.CompletedTask; }, After("crash"));
                await WaitExit(30_000);
                Forget();
                await Case("T22b", "B5", new[] { "stale-descriptor", "no-editor" }, 8_000, 0, null);
            }

            if (Want(only, "B6"))
            {
                await EnsureEditor();
                // A scene backup needs a dirty scene and an entry into Play Mode before the crash.
                await Exec(5000, "statuslab", "dirty_scene");
                S.Actions.Add("playbook", "play-command", "start");
                await Cli(60, "command", "editor.playmode", "start", "--pid", T.Pid.ToString());
                await Task.Delay(3000, Cancel);
                await Exec(5000, "statuslab", "crash");
                await S.WaitFor(r => r.State == "crash", 20_000, Cancel);
                await WaitExit(120_000);
                Forget();
                await Case("T23", "B6", new[] { "stale-descriptor|no-editor", "cold-start|backup-prompt|settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                    600_000, 10_000, StartEditor, After("ready-idle"), mode: SupervisorMode.Live);
            }

            if (Want(only, "B7"))
            {
                await QuitEditor();
                Forget();
                WriteBroken();
                try
                {
                var check = await EditorLauncher.Precheck(T, Cancel);
                S.Recorder.Meta("precheck exit " + check.ExitCode + " in " + check.Ms + " ms, errors " + check.Errors.Count, S.Clock.NowMs);
                // The precheck blocks this start in real use. The case starts anyway, to test the Safe Mode safety net.
                await Case("T24", "B7", new[] { "no-editor", "cold-start", "safe-mode", "cold-start|exited|crash" },
                    180_000, 20_000, StartEditor, After("safe-mode", "exited|crash"), mode: SupervisorMode.Live, key: "safe-mode");
                await WaitExit(30_000);
                Forget();
                }
                finally { DeleteFolder(AssetPath("Broken")); }
                await Case("T24c", "B7", new[] { "no-editor|stale-descriptor", "cold-start|settling|importing|compiling|domain-reload|long-command", "ready-idle" }, 600_000, 0,
                    StartEditor, After("ready-idle"), scored: false);
            }

            if (Want(only, "B8"))
            {
                await QuitEditor();
                Forget();
                WriteHangFlag();
                S.Supervisor.BeforeRestart = () => { DeleteHangFlag(); return Task.CompletedTask; };
                try
                {
                await Case("T25", "B8", new[] { "no-editor", "cold-start", "exited|crash", "no-editor|stale-descriptor|foreign-editor", "cold-start|settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                    900_000, 10_000, StartEditor, After("cold-start", "ready-idle"),
                    expect: "stalled|stuck", mode: SupervisorMode.Live);
                }
                finally
                {
                    S.Supervisor.BeforeRestart = null;
                    DeleteHangFlag();
                }
            }

            if (Want(only, "B10"))
            {
                // Batch-mode compile check before a start: good code, then broken code.
                await QuitEditor();
                Forget();
                foreach (var broken in new[] { false, true })
                {
                    if (broken) WriteBroken();
                    try
                    {
                        var r = await EditorLauncher.Precheck(T, Cancel);
                        S.Recorder.Meta("precheck broken=" + broken + " exit " + r.ExitCode + " in " + r.Ms + " ms, errors " + r.Errors.Count
                            + ": " + string.Join(" | ", r.Errors.Take(3)), S.Clock.NowMs);
                        Console.Error.WriteLine($"precheck broken={broken} exit {r.ExitCode} in {r.Ms} ms, errors {r.Errors.Count}, unity running after: {EditorLauncher.FindUnity(Project).Count}");
                    }
                    finally { if (broken) DeleteFolder(AssetPath("Broken")); }
                }
                await Case("T27", "B10", new[] { "no-editor", "cold-start", "settling|importing|compiling|domain-reload|long-command", "ready-idle" },
                    600_000, 0, StartEditor, After("ready-idle"), scored: false);
            }

            if (Want(only, "B9"))
            {
                // Probe: does -ignoreCompilerErrors open a normal Editor (not Safe Mode) when the code has errors?
                await QuitEditor();
                Forget();
                WriteBroken();
                T.ExtraArgs = "-ignoreCompilerErrors";
                try
                {
                    await Case("T26", "B9", new[] { "no-editor", "cold-start", "settling|importing|compiling|domain-reload|long-command", "compile-failed|ready-idle" },
                        300_000, 0, StartEditor, After("compile-failed|ready-idle|safe-mode"), scored: false, mode: SupervisorMode.DryRun);
                }
                finally
                {
                    T.ExtraArgs = null;
                    DeleteFolder(AssetPath("Broken"));
                }
                await Case("T26c", "B9", new[] { "compile-failed|ready-idle|safe-mode", "compiling|importing|domain-reload|long-command", "ready-idle" }, 180_000, 0,
                    () => { Recompile(); return Task.CompletedTask; }, After("ready-idle"), scored: false);
            }
        }

        void DeleteLibrary()
        {
            var library = Path.Combine(Project, "Library");
            if (Directory.Exists(library)) Directory.Delete(library, true);
        }
    }
}
