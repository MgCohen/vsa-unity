using System.Globalization;
using System.Text.Json;

namespace VSA.TestSuite.Client
{
    /// <summary>A failed action. It has the code, the reason and the step folder with the evidence.</summary>
    public sealed class ActionFailedException : Exception
    {
        public string Action { get; }
        public string Code { get; }
        public string Reason { get; }
        public string Folder { get; }
        public JsonElement Detail { get; }

        public ActionFailedException(string action, string code, string reason, string folder, JsonElement detail)
            : base(action + " failed: " + code + ": " + reason)
        {
            Action = action;
            Code = code;
            Reason = reason;
            Folder = folder;
            Detail = detail;
        }
    }

    /// <summary>A command that the Pipeline refused or could not run, for example an unknown command.</summary>
    public sealed class CommandFailedException : Exception
    {
        public ExecResult Exec { get; }
        public CommandFailedException(string command, ExecResult exec) : base(command + ": " + exec) => Exec = exec;
    }

    /// <summary>A running recording. Dispose stops it. A second stop does nothing.</summary>
    public sealed class RecordingHandle : IAsyncDisposable
    {
        readonly Game game;
        JsonElement? stopped;

        /// <summary>The result of record.start: the file and the start time.</summary>
        public JsonElement Started { get; }
        /// <summary>The video file.</summary>
        public string File => Started.GetProperty("File").GetString();

        internal RecordingHandle(Game game, JsonElement started)
        {
            this.game = game;
            Started = started;
        }

        /// <summary>Stops the recording and returns the result of record.stop.</summary>
        public async Task<JsonElement> Stop()
        {
            stopped ??= await game.StopRecording();
            return stopped.Value;
        }

        public async ValueTask DisposeAsync()
        {
            if (stopped != null) return;
            try
            {
                await Stop();
            }
            catch (Exception e)
            {
                // The instance can be gone. The recorder stops by itself at Play Mode exit and at its time limit.
                Console.Error.WriteLine("Recording stop failed: " + e.Message);
            }
        }
    }

    /// <summary>
    /// The client DSL: one method for each action. Each method sends the CLI command of the action,
    /// so the call goes through the same commands, actions, systems and hooks as the CLI.
    /// </summary>
    public sealed class Game : IDisposable
    {
        public ITransport Transport { get; }
        public Health Health { get; }

        /// <summary>The time limit of one command in the Editor, in milliseconds.</summary>
        public int CommandTimeoutMs { get; set; } = 30000;

        public Game(ITransport transport)
        {
            Transport = transport;
            Health = new Health(transport);
        }

        /// <summary>Attaches to the Editor of a project with direct HTTP, and waits until it is ready.</summary>
        public static async Task<Game> AttachEditor(string projectPath, int readyTimeoutMs = 120000)
        {
            var game = new Game(new HttpTransport(Instance.ForEditor(projectPath)));
            await game.Health.WaitReady(readyTimeoutMs);
            return game;
        }

        /// <summary>The player process that this client started. Null for an Editor or an attached player.</summary>
        public System.Diagnostics.Process OwnedProcess { get; private set; }

        /// <summary>Attaches to a running player that writes its descriptor to <paramref name="playerFolder"/>.</summary>
        public static async Task<Game> AttachPlayer(string playerFolder, int readyTimeoutMs = 60000)
        {
            var game = new Game(new HttpTransport(Instance.ForPlayer(playerFolder)));
            await game.Health.WaitReady(readyTimeoutMs);
            return game;
        }

        /// <summary>
        /// Starts a development player and waits until it is ready. Give each player its own folder:
        /// two players in one folder write the same descriptor file.
        /// </summary>
        public static async Task<Game> LaunchPlayer(string exePath, string arguments = "-screen-fullscreen 0", int readyTimeoutMs = 60000)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(exePath));
            var instance = Instance.ForPlayer(folder);
            // A descriptor from an earlier run points at a dead process or at another player.
            if (File.Exists(instance.DescriptorPath)) File.Delete(instance.DescriptorPath);

            // Without -logFile, the player writes its log to the console of the test.
            if (!arguments.Contains("-logFile")) arguments += " -logFile \"" + Path.Combine(folder, "player.log") + "\"";
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exePath, arguments)
            {
                WorkingDirectory = folder,
                UseShellExecute = false,
                // Unity prints some lines to the console before it opens the log file. Drop them.
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var game = new Game(new HttpTransport(instance)) { OwnedProcess = process };
            try
            {
                await game.Health.WaitReady(readyTimeoutMs);
            }
            catch
            {
                game.StopPlayer();
                throw;
            }
            return game;
        }

        /// <summary>Stops the player that this client started. It never stops other processes.</summary>
        public void StopPlayer(int graceMs = 5000)
        {
            var p = OwnedProcess;
            if (p == null || p.HasExited) return;
            p.CloseMainWindow();
            if (!p.WaitForExit(graceMs)) p.Kill();
            OwnedProcess = null;
        }

        public Task<JsonElement> Info() => Act("game.info");

        // ui
        public Task<JsonElement> FindObject(string path) => Act("ui.find_object", "--path", path);
        public Task<JsonElement> ClickObject(string path) => Act("ui.click_object", "--path", path);
        public Task<JsonElement> ClickScreen(float x, float y) => Act("ui.click_screen", "--x", F(x), "--y", F(y));
        public Task<JsonElement> WaitForObject(string path, string state = "Active", int timeoutMs = 10000)
            => Act("ui.wait_for_object", "--path", path, "--state", state, "--timeout-ms", timeoutMs.ToString(CultureInfo.InvariantCulture));
        public Task<JsonElement> AssertState(string path, string state = "Active") => Act("ui.assert_state", "--path", path, "--state", state);
        public Task<JsonElement> AssertText(string path, string text) => Act("ui.assert_text", "--path", path, "--text", text);
        public Task<JsonElement> HoldObject(string path, int ms) => Act("ui.hold_object", "--path", path, "--ms", I(ms));
        public Task<JsonElement> HoldScreen(float x, float y, int ms) => Act("ui.hold_screen", "--x", F(x), "--y", F(y), "--ms", I(ms));
        public Task<JsonElement> DragObject(string path, string toPath, int steps = 10) => Act("ui.drag_object", "--path", path, "--to", toPath, "--steps", I(steps));
        public Task<JsonElement> DragScreen(float x, float y, float toX, float toY, int steps = 10)
            => Act("ui.drag_screen", "--x", F(x), "--y", F(y), "--to-x", F(toX), "--to-y", F(toY), "--steps", I(steps));

        /// <summary>Shows or hides the touch overlay: a burst at each press and a trail for each drag.</summary>
        public Task<JsonElement> ShowTouches(bool on = true) => Act("ui.show_touches", "--on", on ? "true" : "false");

        // game
        public Task<JsonElement> WaitFrames(int count) => Act("game.wait_frames", "--count", count.ToString(CultureInfo.InvariantCulture));
        public Task<JsonElement> WaitSeconds(double seconds) => Act("game.wait_seconds", "--seconds", F(seconds));
        public Task<JsonElement> LoadScene(string scene) => Act("game.load_scene", "--scene", scene);

        /// <summary>Runs the reset steps, sets the seed, and loads <paramref name="scene"/> (null: reload the active scene).</summary>
        public Task<JsonElement> Reset(string scene = null, int? seed = null)
            => Act("game.reset", "--scene", scene ?? "", "--seed", seed.HasValue ? I(seed.Value) : "");

        // suite
        /// <summary>Runs a C# test script inside Unity. Fails with the code of the first failed step.</summary>
        public Task<JsonElement> RunScript(string script) => Act("suite.run", "--script", script);
        public Task<JsonElement> ListScripts() => Act("suite.list");

        // record
        /// <summary>Starts a video recording in the step folder of the command. A running recording stops first.</summary>
        public Task<JsonElement> StartRecording(string name = "session", int maxSeconds = 0)
            => Act("record.start", "--name", name, "--max-seconds", I(maxSeconds));

        /// <summary>Stops the video recording. With no recording, it returns the last one. It is safe to call at all times.</summary>
        public Task<JsonElement> StopRecording() => Act("record.stop");

        /// <summary>The recorder, the current recording and the last recording.</summary>
        public Task<JsonElement> RecordingStatus() => Act("record.status");

        /// <summary>
        /// Starts a recording and returns a handle. Use it with <c>await using</c>: the dispose stops
        /// the recording, also when the test throws.
        /// </summary>
        public async Task<RecordingHandle> Record(string name = "session", int maxSeconds = 0)
            => new RecordingHandle(this, await StartRecording(name, maxSeconds));

        /// <summary>Enters Play Mode and waits until the player loop runs frames.</summary>
        public async Task EnterPlayMode(int timeoutMs = 60000)
        {
            await Command("editor.playmode", "start");
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (true)
            {
                var r = await Health.Probe();
                if (r.State == HealthState.Ready && r.PlayerLoopTicking == true) return;
                if (r.State == HealthState.Dialog || r.State == HealthState.Dead || DateTime.UtcNow > until)
                    throw new InstanceException(Transport.Instance, "Play Mode did not start: " + r);
                await Task.Delay(300);
            }
        }

        public Task ExitPlayMode() => Command("editor.playmode", "stop");

        /// <summary>Runs any Pipeline command and returns its result. Fails if the command fails.</summary>
        public async Task<JsonElement> Command(params string[] argv)
        {
            var r = await Exec(argv);
            if (!r.Success) throw new CommandFailedException(string.Join(" ", argv), r);
            return r.Result;
        }

        /// <summary>Runs one of our actions. Returns its "result" value, or throws ActionFailedException.</summary>
        public async Task<JsonElement> Act(params string[] argv)
        {
            var r = await Exec(argv);
            var body = r.Result;
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("ok", out var ok))
            {
                if (ok.ValueKind == JsonValueKind.True)
                    return body.TryGetProperty("result", out var result) ? result : default;
                throw new ActionFailedException(
                    Str(body, "action"), Str(body, "code"), Str(body, "reason"), Str(body, "folder"),
                    body.TryGetProperty("detail", out var detail) ? detail : default);
            }
            throw new CommandFailedException(string.Join(" ", argv), r);
        }

        async Task<ExecResult> Exec(string[] argv)
        {
            try
            {
                return await Transport.Exec(argv, CommandTimeoutMs);
            }
            catch (TimeoutException e)
            {
                // The command did not answer. Find out why before the test reports it.
                var health = await Health.Probe();
                throw new InstanceException(Transport.Instance, e.Message + ". Health: " + health);
            }
        }

        static string Str(JsonElement e, string name)
            => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
        static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

        public void Dispose()
        {
            StopPlayer();
            (Transport as IDisposable)?.Dispose();
        }
    }
}
