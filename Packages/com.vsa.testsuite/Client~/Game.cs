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

        // ui
        public Task<JsonElement> FindObject(string path) => Act("ui.find_object", "--path", path);
        public Task<JsonElement> ClickObject(string path) => Act("ui.click_object", "--path", path);
        public Task<JsonElement> ClickScreen(float x, float y) => Act("ui.click_screen", "--x", F(x), "--y", F(y));
        public Task<JsonElement> WaitForObject(string path, string state = "Active", int timeoutMs = 10000)
            => Act("ui.wait_for_object", "--path", path, "--state", state, "--timeout-ms", timeoutMs.ToString(CultureInfo.InvariantCulture));
        public Task<JsonElement> AssertState(string path, string state = "Active") => Act("ui.assert_state", "--path", path, "--state", state);
        public Task<JsonElement> AssertText(string path, string text) => Act("ui.assert_text", "--path", path, "--text", text);

        // game
        public Task<JsonElement> WaitFrames(int count) => Act("game.wait_frames", "--count", count.ToString(CultureInfo.InvariantCulture));
        public Task<JsonElement> WaitSeconds(double seconds) => Act("game.wait_seconds", "--seconds", F(seconds));

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

        static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

        public void Dispose() => (Transport as IDisposable)?.Dispose();
    }
}
