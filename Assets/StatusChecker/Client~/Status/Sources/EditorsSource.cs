using System.Text.Json;

namespace VSA.StatusChecker
{
    /// <summary>editors: "unity pipeline editors --format json", the row of the own PID. The row has no phase
    /// field in CLI 1.0.1, so the source writes "safe mode", "reachable" or "unreachable".
    /// It takes about 5 s, so it runs on its own task about every 6 s.</summary>
    public sealed class EditorsSource : ISignalSource
    {
        readonly CliPoller poller = new("pipeline editors --format json");

        public string Key => "editors";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            poller.Kick(target);
            var json = JsonFind.Parse(poller.Last);
            if (UnityStatusSource.OwnRow(json, target.Pid) is not JsonElement row) return Task.CompletedTask;
            var phase = JsonFind.Str(row, "phase");
            var safe = JsonFind.Find(row, "safeMode");
            if (phase == null && safe is JsonElement s && s.ValueKind != JsonValueKind.Null) phase = "safe mode";
            phase ??= JsonFind.Bool(JsonFind.Find(row, "pipelineServer"), "isReachable") == true ? "reachable" : "unreachable";
            into.EditorsPhase = phase;
            return Task.CompletedTask;
        }
    }
}
