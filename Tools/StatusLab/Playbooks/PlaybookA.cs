using VSA.TestSuite.Client.Status;
namespace StatusLab
{
    /// <summary>Playbook A: one Editor stays open. Steps A1-A12, test cases T01-T16.</summary>
    public sealed class PlaybookA : PlaybookRunner
    {

        protected override async Task Steps(ISet<string> only)
        {
            await EnsureEditor();

            if (Want(only, "A1"))
                await Case("T01", "A1", new[] { "ready-idle" }, 30_000, 0, null);

            if (Want(only, "A2"))
            {
                var folder = AssetPath("ImportLoad");
                await Case("T02", "A2", new[] { "ready-idle", "long-command|frozen|importing", "ready-idle" }, 60_000, 10_000, async () =>
                {
                    Directory.CreateDirectory(folder);
                    // Two files of the lab importer: each import takes 5 s (SlowImporter).
                    for (var i = 0; i < 2; i++) File.WriteAllText(Path.Combine(folder, "load" + i + ".slowimport"), "slow " + Environment.TickCount + " " + i);
                    await Exec(5000, "statuslab", "refresh");
                }, After("importing", "ready-idle"), key: "importing");
                await Case("T02c", "A2", new[] { "ready-idle", "importing", "ready-idle" }, 60_000, 0, async () =>
                {
                    DeleteFolder(folder);
                    await Exec(5000, "statuslab", "refresh");
                }, After("importing", "ready-idle"), scored: false);
            }

            if (Want(only, "A3"))
                await Case("T03", "A3", new[] { "ready-idle", "compiling|importing|domain-reload|long-command", "ready-idle" }, 120_000, 10_000, () =>
                {
                    TouchCode(Environment.TickCount & 0x7fffffff);
                    Recompile();
                    return Task.CompletedTask;
                }, After("domain-reload", "ready-idle"), key: "compiling");

            if (Want(only, "A4"))
            {
                await Case("T05", "A4", new[] { "ready-idle", "compiling", "compile-failed" }, 90_000, 10_000, () =>
                {
                    WriteBroken();
                    Recompile();
                    return Task.CompletedTask;
                }, After("compile-failed"));
                await Case("T05c", "A4", new[] { "compile-failed", "compiling|importing|domain-reload|long-command", "ready-idle" }, 120_000, 0, () =>
                {
                    DeleteFolder(AssetPath("Broken"));
                    Recompile();
                    return Task.CompletedTask;
                }, After("ready-idle"), scored: false);
            }

            if (Want(only, "A5"))
            {
                await Case("T06", "A5", new[] { "ready-idle", "importing|play-transition|long-command|frozen", "play-running" }, 60_000, 10_000, () =>
                {
                    S.Actions.Add("playbook", "play-command", "start");
                    _ = Cli(60, "command", "editor.playmode", "start", "--pid", T.Pid.ToString());
                    return Task.CompletedTask;
                }, After("play-running"));
                await Case("T07", "A5", new[] { "play-running" }, 15_000, 0, null);
                await Case("T08", "A5", new[] { "play-running", "play-paused" }, 10_000, 3_000,
                    async () => await Exec(5000, "statuslab", "pause", "--on", "true"));
                await Case("T08c", "A5", new[] { "play-paused", "play-running" }, 8_000, 0,
                    async () => await Exec(5000, "statuslab", "pause", "--on", "false"), scored: false);
                await Case("T06b", "A5", new[] { "play-running|play-paused", "importing|play-transition|long-command|frozen|domain-reload", "ready-idle" }, 60_000, 10_000, () =>
                {
                    S.Actions.Add("playbook", "play-command", "stop");
                    _ = Cli(60, "command", "editor.playmode", "stop", "--pid", T.Pid.ToString());
                    return Task.CompletedTask;
                }, After("ready-idle"));
            }

            if (Want(only, "A6"))
                await Case("T09", "A6", new[] { "ready-idle", "long-command", "ready-idle" }, 45_000, 5_000, () =>
                {
                    S.Actions.Add("playbook", "job-start", "statuslab hold 30");
                    _ = Task.Run(async () =>
                    {
                        await Exec(40_000, "statuslab", "hold", "--seconds", "30");
                        S.Actions.Add("playbook", "job-end", "statuslab hold 30");
                    });
                    return Task.CompletedTask;
                }, After("long-command", "ready-idle"), key: "long-command");

            if (Want(only, "A7"))
                await Case("T10", "A7", new[] { "ready-idle", "long-command", "frozen", "ready-idle" }, 110_000, 5_000,
                    async () => await Exec(5000, "statuslab", "freeze", "--seconds", "90"),
                    After("frozen", "ready-idle"), expect: "stalled", mode: SupervisorMode.DryRun, key: "frozen");

            if (Want(only, "A8"))
                await Case("T11", "A8", new[] { "ready-idle", "long-command", "frozen", "ready-idle" }, 110_000, 5_000,
                    async () => await Exec(5000, "statuslab", "spin", "--seconds", "90"),
                    After("frozen", "ready-idle"), expect: "stalled", mode: SupervisorMode.DryRun, key: "frozen");

            if (Want(only, "A9"))
                await Case("T12", "A9", new[] { "ready-idle", "native-dialog", "ready-idle" }, 30_000, 3_000,
                    async () => await Exec(5000, "statuslab", "native_dialog"),
                    After("native-dialog", "ready-idle"), mode: SupervisorMode.Live, key: "native-dialog");

            if (Want(only, "A10"))
            {
                await Case("T13", "A10", new[] { "ready-idle" }, 10_000, 0,
                    async () => await Exec(5000, "statuslab", "display_dialog"));
                await Case("T13b", "A10", new[] { "ready-idle" }, 10_000, 0, async () =>
                {
                    await Exec(5000, "statuslab", "dirty_scene");
                    await Exec(5000, "statuslab", "save_prompt");
                });
                await Case("T13c", "A10", new[] { "ready-idle" }, 5_000, 0,
                    async () => await Exec(10_000, "statuslab", "clean_scenes", "--mode", "discard"), scored: false);
            }

            if (Want(only, "A11"))
            {
                // T14: a dirty scene, then an Edit Mode test run with no save rule. T15: the save rule first.
                const string Test = "ActionNameComesFromTheAttribute";
                await Case("T14", "A11", new[] { "ready-idle", "long-command|frozen|compiling|importing|domain-reload", "ready-idle" }, 120_000, 0, async () =>
                {
                    await Exec(5000, "statuslab", "dirty_scene");
                    await Cli(110, "command", "test", "run", "--mode", "editor", "--filter", Test, "--timeout", "90", "--pid", T.Pid.ToString());
                }, scored: false);
                await Case("T15", "A11", new[] { "ready-idle", "long-command|frozen|compiling|importing|domain-reload", "ready-idle" }, 120_000, 0, async () =>
                {
                    await Exec(5000, "statuslab", "dirty_scene");
                    await Exec(10_000, "statuslab", "clean_scenes", "--mode", "discard");
                    await Cli(110, "command", "test", "run", "--mode", "editor", "--filter", Test, "--timeout", "90", "--pid", T.Pid.ToString());
                }, scored: false);
            }

            if (Want(only, "A12"))
            {
                S.Supervisor.BeforeRestart = () => { DeleteHangFlag(); return Task.CompletedTask; };
                await Case("T16", "A12",
                    new[] { "ready-idle", "compiling|importing|domain-reload|long-command|frozen|cold-start", "exited|crash", "no-editor|stale-descriptor|foreign-editor", "cold-start|settling|importing|long-command", "ready-idle" },
                    600_000, 10_000, () =>
                    {
                        WriteHangFlag();
                        TouchCode(Environment.TickCount & 0x7fffffff);
                        Recompile();
                        return Task.CompletedTask;
                    }, After("domain-reload|frozen|cold-start", "ready-idle"), expect: "stalled|stuck", mode: SupervisorMode.Live);
                S.Supervisor.BeforeRestart = null;
                DeleteHangFlag();
            }
        }

        /// <summary>A 24-bit BMP with random pixels. Textures take seconds to import, so a 500 ms sampler sees the import.</summary>
        static void WriteNoiseBmp(string path, int size, int seed)
        {
            var row = size * 3;
            var data = new byte[54 + row * size];
            void Int(int at, int v) => BitConverter.GetBytes(v).CopyTo(data, at);
            data[0] = (byte)'B'; data[1] = (byte)'M';
            Int(2, data.Length); Int(10, 54); Int(14, 40); Int(18, size); Int(22, size);
            data[26] = 1; data[28] = 24; Int(34, row * size);
            new Random(seed).NextBytes(data.AsSpan(54));
            File.WriteAllBytes(path, data);
        }
    }
}
