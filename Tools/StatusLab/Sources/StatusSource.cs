namespace StatusLab
{
    /// <summary>status: GET /api/status. Served off the main thread.</summary>
    public sealed class StatusSource : ISignalSource
    {
        public string Key => "status";

        public async Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            var (http, body, _) = await PipelineHttp.Get(target, "/api/status", 1000, cancel);
            into.StatusHttp = http;
            var json = JsonFind.Parse(body);
            into.Status = JsonFind.Str(json, "status");
            into.PlayerLoopTicking = JsonFind.Bool(json, "playerLoopTicking");
        }
    }
}
