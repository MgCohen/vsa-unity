using System.Text.Json;

namespace StatusLab
{
    /// <summary>u-status: "unity status --format json", the row of the own PID. It takes about 5 s,
    /// so it runs on its own task about every 6 s.</summary>
    public sealed class UnityStatusSource : ISignalSource
    {
        readonly CliPoller poller = new("status --format json");

        public string Key => "u-status";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            poller.Kick(target);
            into.CliAgeMs = poller.AgeMs;
            var json = JsonFind.Parse(poller.Last);
            if (json == null) return Task.CompletedTask;
            var code = JsonFind.Str(JsonFind.Find(json, "errors"), "code");
            into.UnityStatus = OwnRow(json, target.Pid) is JsonElement row ? JsonFind.Str(row, "state") : code ?? "no row";
            return Task.CompletedTask;
        }

        /// <summary>The row of data.items whose pid is the own PID.</summary>
        public static JsonElement? OwnRow(JsonElement? json, int pid)
        {
            if (JsonFind.Find(json, "items") is not JsonElement items || items.ValueKind != JsonValueKind.Array) return null;
            foreach (var item in items.EnumerateArray())
                if (JsonFind.Int(item, "pid") == pid) return item;
            return null;
        }
    }
}
