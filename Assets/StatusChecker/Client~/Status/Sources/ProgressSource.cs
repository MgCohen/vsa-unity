namespace VSA.StatusChecker
{
    /// <summary>progress: GET /api/progress. Served off the main thread.</summary>
    public sealed class ProgressSource : ISignalSource
    {
        public string Key => "progress";

        public async Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            var (http, body, _) = await PipelineHttp.Get(target, "/api/progress", 1000, cancel);
            if (http != 200) return;
            var json = JsonFind.Parse(body);
            into.ProgressActive = JsonFind.Bool(json, "active");
            if (into.ProgressActive == true)
            {
                var title = JsonFind.Str(json, "title");
                var info = JsonFind.Str(json, "info");
                into.ProgressText = (title + " " + info).Trim();
            }
        }
    }
}
