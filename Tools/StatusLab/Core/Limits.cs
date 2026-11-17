using System.Text.Json;

namespace StatusLab
{
    /// <summary>Cap and stall window of each waiting state, from limits.json. A state with no entry does not wait.
    /// A cap of 0 means: no cap, only the stall window.</summary>
    public sealed class Limits
    {
        readonly Dictionary<string, (long CapMs, long StallMs)> map = new();

        public static Limits Load(string path = null)
        {
            path ??= Path.Combine(AppContext.BaseDirectory, "Core", "limits.json");
            var limits = new Limits();
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var p in doc.RootElement.EnumerateObject())
                limits.map[p.Name] = (p.Value.GetProperty("capS").GetInt64() * 1000, p.Value.GetProperty("stallS").GetInt64() * 1000);
            return limits;
        }

        public bool TryGet(string state, bool fullImport, out long capMs, out long stallMs)
        {
            var key = state == "cold-start" && fullImport ? "cold-start-full" : state;
            var found = map.TryGetValue(key, out var v);
            capMs = v.CapMs;
            stallMs = v.StallMs;
            return found;
        }
    }
}
