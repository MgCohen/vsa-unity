using System.Globalization;
using System.Text.Json;

namespace VSA.StatusChecker
{
    /// <summary>beacon: beacon.json in the status folder, written by the StatusBeacon of the package. Accepted only if its pid is the own PID.</summary>
    public sealed class BeaconSource : ISignalSource
    {
        public string Key => "beacon";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (target.StatusFolder == null) return Task.CompletedTask;
            var path = Path.Combine(target.StatusFolder, "beacon.json");
            if (!File.Exists(path)) return Task.CompletedTask;
            JsonElement? json;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                json = JsonFind.Parse(reader.ReadToEnd());
            }
            catch (IOException) { return Task.CompletedTask; }
            if (json == null || JsonFind.Int(json, "pid") != target.Pid || target.Pid <= 0) return Task.CompletedTask;
            into.BeaconOwn = true;
            into.BeaconSeq = JsonFind.Find(json, "seq")?.GetInt64() ?? 0;
            into.BeaconAgeS = Age(JsonFind.Str(json, "utc"));
            into.BeaconCompiling = JsonFind.Bool(json, "compiling");
            into.BeaconUpdating = JsonFind.Bool(json, "updating");
            into.BeaconPlaying = JsonFind.Bool(json, "playing");
            into.BeaconPaused = JsonFind.Bool(json, "paused");
            into.BeaconDirtyScenes = JsonFind.Int(json, "dirtyScenes");
            if (JsonFind.Find(json, "events") is JsonElement events && events.ValueKind == JsonValueKind.Array && events.GetArrayLength() > 0)
            {
                var last = events[events.GetArrayLength() - 1];
                into.BeaconLastEvent = JsonFind.Str(last, "name");
                into.BeaconLastEventAgeS = Age(JsonFind.Str(last, "utc"));
            }
            return Task.CompletedTask;
        }

        static double? Age(string utc)
            => DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
                ? Math.Round((DateTime.UtcNow - at.ToUniversalTime()).TotalSeconds, 2)
                : null;
    }
}
