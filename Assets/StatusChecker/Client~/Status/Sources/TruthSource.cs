namespace VSA.StatusChecker
{
    /// <summary>truth: "console status" groundTruth. Runs only after a good probe, and at most every 2 s.</summary>
    public sealed class TruthSource : ISignalSource
    {
        DateTime last;
        bool? compiling, failed;
        int? errors;

        public string Key => "truth";

        public async Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (into.ProbeOk && DateTime.UtcNow - last > TimeSpan.FromSeconds(2))
            {
                last = DateTime.UtcNow;
                var (http, body, _) = await PipelineHttp.Exec(target, new[] { "console", "status" }, 2000, cancel);
                if (http == 200)
                {
                    var truth = JsonFind.Find(JsonFind.Parse(body), "groundTruth");
                    compiling = JsonFind.Bool(truth, "compiling");
                    failed = JsonFind.Bool(truth, "compilationFailed");
                    errors = JsonFind.Int(truth, "consoleErrors");
                }
            }
            if (!into.ProbeOk) return;
            into.TruthCompiling = compiling;
            into.TruthFailed = failed;
            into.TruthErrors = errors;
        }
    }
}
