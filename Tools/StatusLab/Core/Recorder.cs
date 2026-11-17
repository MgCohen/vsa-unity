using System.Text.Json;
using System.Text.Json.Serialization;

namespace StatusLab
{
    /// <summary>Writes one NDJSON file: samples with their result, truth windows and actions, in time order.</summary>
    public sealed class Recorder : IDisposable
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
        };

        readonly StreamWriter writer;
        readonly object gate = new();

        public string Path { get; }

        public Recorder(string path)
        {
            Path = path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            writer = new StreamWriter(path, append: true) { AutoFlush = true };
        }

        public void Sample(Snapshot s, StateResult r) => Write(new { kind = "sample", t = s.T, s, r });

        public void Truth(TruthWindow w) => Write(new { kind = "truth", t = w.T0, w });

        public void Action(ActionEntry a) => Write(new { kind = "action", t = a.T, a });

        public void Meta(string text, long t) => Write(new { kind = "meta", t, text });

        void Write(object line)
        {
            var json = JsonSerializer.Serialize(line, Options);
            lock (gate) writer.WriteLine(json);
        }

        public void Dispose() => writer.Dispose();
    }
}
