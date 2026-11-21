using System.Text.Json;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>The limits of the waiting states, from limits.json. Each state in "states" is a waiting state.
    /// "capS": the maximum time in the state; 0 means no cap. "stallS": the maximum time with no progress;
    /// a state with no "stallS" uses "defaultStallS". A state that is not in the file does not wait.</summary>
    public sealed class Limits
    {
        readonly Dictionary<string, (long CapMs, long StallMs)> map = new();

        public long DefaultStallMs { get; private set; } = 120_000;

        /// <summary>The name of the embedded default limits.json.</summary>
        public const string ResourceName = "VSA.TestSuite.Client.Status.limits.json";

        /// <summary>Loads the limits from a file, or the embedded default limits.json if path is null.</summary>
        public static Limits Load(string path = null) => Parse(path == null ? ReadDefault() : File.ReadAllText(path));

        /// <summary>The text of the embedded default limits.json.</summary>
        public static string ReadDefault()
        {
            using var stream = typeof(Limits).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException("The resource " + ResourceName + " is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static Limits Parse(string json)
        {
            var limits = new Limits();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("defaultStallS", out var stall)) limits.DefaultStallMs = stall.GetInt64() * 1000;
            foreach (var p in root.GetProperty("states").EnumerateObject())
            {
                var cap = p.Value.TryGetProperty("capS", out var c) ? c.GetInt64() * 1000 : 0;
                var stallMs = p.Value.TryGetProperty("stallS", out var s) ? s.GetInt64() * 1000 : limits.DefaultStallMs;
                limits.map[p.Name] = (cap, stallMs);
            }
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
