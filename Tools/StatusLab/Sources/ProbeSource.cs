namespace StatusLab
{
    /// <summary>probe: "statuslab ping" through /api/exec with timeout 2000 ms. A queued probe that does not start
    /// gets HTTP 504 and never runs, so a freeze never fills the queue.</summary>
    public sealed class ProbeSource : ISignalSource
    {
        public const int TimeoutMs = 2000;

        public string Key => "probe";

        public async Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (into.StatusHttp != 200) return;
            var (http, body, ms) = await PipelineHttp.Exec(target, new[] { "statuslab", "ping" }, TimeoutMs, cancel);
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
        }
    }
}
