namespace VSA.TestSuite.Client.Status
{
    /// <summary>probe: "suite ping" through /api/exec with timeout 2000 ms. A queued probe that does not start
    /// gets HTTP 504 and never runs, so a freeze never fills the queue. If the Editor has no "suite ping"
    /// (the package is missing), the probe uses GET /api/editor_status, which also needs the main thread.</summary>
    public sealed class ProbeSource : ISignalSource
    {
        public const int TimeoutMs = 2000;

        public string Key => "probe";

        public async Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (into.StatusHttp != 200) return;
            if (target.ProbeMissing)
            {
                await Fallback(into, target, cancel);
                return;
            }
            var (http, body, ms) = await PipelineHttp.Exec(target, target.Probe ?? Target.EditorProbe, TimeoutMs, cancel);
            if (UnknownCommand(http, body))
            {
                target.ProbeMissing = true;
                await Fallback(into, target, cancel);
                return;
            }
            into.ProbeHttp = http;
            into.ProbeMs = Math.Round(ms, 1);
            var json = JsonFind.Parse(body);
            into.ProbeBusyReason = JsonFind.Str(json, "busyReason");
            into.ProbeOk = http == 200 && JsonFind.Int(json, "frame") != null;
            if (!into.ProbeOk) return;
            into.ProbeFrame = JsonFind.Int(json, "frame");
            into.ProbeCompiling = JsonFind.Bool(json, "compiling");
            into.ProbeUpdating = JsonFind.Bool(json, "updating");
            into.ProbePlaying = JsonFind.Bool(json, "playing");
            into.ProbePaused = JsonFind.Bool(json, "paused");
            into.ProbeDirtyScenes = JsonFind.Int(json, "dirtyScenes");
            if (JsonFind.Find(json, "dirtyScenePaths") is System.Text.Json.JsonElement paths && paths.ValueKind == System.Text.Json.JsonValueKind.Array)
                into.ProbeDirtyScenePaths = string.Join("|", paths.EnumerateArray().Select(p => p.GetString()));
        }

        static async Task Fallback(Snapshot into, Target target, CancellationToken cancel)
        {
            var (http, body, ms) = await PipelineHttp.Get(target, "/api/editor_status", TimeoutMs, cancel);
            into.ProbeHttp = http;
            into.ProbeMs = Math.Round(ms, 1);
            into.ProbeFallback = true;
            into.ProbeOk = http == 200;
            if (!into.ProbeOk) return;
            var json = JsonFind.Parse(body);
            into.ProbeCompiling = JsonFind.Bool(json, "isCompiling") ?? JsonFind.Bool(json, "compiling");
            into.ProbeUpdating = JsonFind.Bool(json, "isUpdating") ?? JsonFind.Bool(json, "updating");
            into.ProbePlaying = JsonFind.Bool(json, "isPlaying") ?? JsonFind.Bool(json, "playing");
            into.ProbePaused = JsonFind.Bool(json, "isPaused") ?? JsonFind.Bool(json, "paused");
        }

        /// <summary>True if the server says that the probe command does not exist.</summary>
        static bool UnknownCommand(int http, string body)
            => http is 400 or 404 || (http == 200 && body != null && body.Contains("No command named", StringComparison.OrdinalIgnoreCase))
                || (body != null && body.Contains("unknown command", StringComparison.OrdinalIgnoreCase));
    }
}
