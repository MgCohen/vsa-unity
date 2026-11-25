using System.Reflection;
using System.Text.Json;

namespace VSA.StatusChecker
{
    /// <summary>Replays one recording through a fresh status tracker with the recorded time and actions,
    /// then checks each truth window (V3) and collects the signal values seen in each state (V2).</summary>
    public sealed class Replayer
    {
        static readonly FieldInfo[] Fields = typeof(Snapshot).GetFields()
            .Where(f => f.Name != "T" && f.Name != "LogLines" && f.Name != "ProcCpuMs" && f.Name != "LogSize" && f.Name != "BeaconSeq" && f.Name != "CliAgeMs")
            .ToArray();

        /// <summary>state -> signal -> values seen. Filled by Replay.</summary>
        public readonly Dictionary<string, Dictionary<string, HashSet<string>>> Matrix = new();

        /// <summary>Corrected truth for a case id (for example "T20"), from truth-overrides.json. Recordings keep the
        /// truth of their run; a correction found later is written here with its reason.</summary>
        public Dictionary<string, TruthWindow> Overrides { get; } = new();

        /// <summary>Reads truth-overrides.json: case id to corrected truth. The key "_reason" is a comment.</summary>
        public void LoadOverrides(string path)
        {
            foreach (var p in JsonDocument.Parse(File.ReadAllText(path)).RootElement.EnumerateObject())
                if (p.Name != "_reason") Overrides[p.Name] = p.Value.Deserialize<TruthWindow>(Recorder.Options);
        }

        /// <summary>Replays each recording (*.ndjson, *.ndjson.gz) in a folder, in name order.</summary>
        public List<CaseReport> ReplayFolder(string folder)
            => Directory.GetFiles(folder, "*.ndjson*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal)
                .SelectMany(Replay).ToList();

        public List<CaseReport> Replay(string path)
        {
            var clock = new ReplayClock();
            var actions = new ActionLog(clock);
            var tracker = new StatusTracker(clock, actions, Limits.Load());
            var timeline = new List<(long T, StateResult R)>();
            var windows = new List<TruthWindow>();

            foreach (var line in ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                switch (root.GetProperty("kind").GetString())
                {
                    case "action":
                        actions.Add(root.GetProperty("a").Deserialize<ActionEntry>(Recorder.Options));
                        break;
                    case "truth":
                        var w = root.GetProperty("w").Deserialize<TruthWindow>(Recorder.Options);
                        var id = w.Case.Substring(w.Case.IndexOf('/') + 1);
                        if (Overrides.TryGetValue(id, out var o))
                        {
                            if (o.Allowed is { Count: > 0 }) w.Allowed = o.Allowed;
                            if (o.DetectMs > 0) w.DetectMs = o.DetectMs;
                            if (o.Key != null) w.Key = o.Key;
                        }
                        windows.Add(w);
                        break;
                    case "sample":
                        var s = root.GetProperty("s").Deserialize<Snapshot>(Recorder.Options);
                        Normalize(s);
                        clock.NowMs = s.T;
                        var r = tracker.Observe(s);
                        timeline.Add((s.T, r));
                        Collect(r.State, s);
                        break;
                }
            }
            return windows.Select(w => Check(Path.GetFileName(path), w, timeline)).ToList();
        }

        /// <summary>Lines of a recording, plain or gzip (.ndjson.gz).</summary>
        static IEnumerable<string> ReadLines(string path)
        {
            using var file = File.OpenRead(path);
            using Stream stream = path.EndsWith(".gz") ? new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress) : file;
            using var reader = new StreamReader(stream);
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) yield return line;
        }

        static CaseReport Check(string file, TruthWindow w, List<(long T, StateResult R)> timeline)
        {
            var inside = timeline.Where(x => x.T >= w.T0 && x.T <= w.T1).ToList();
            var observed = new List<string>();
            // The state at the trigger is the last state before T0.
            var before = timeline.LastOrDefault(x => x.T < w.T0);
            if (before.R != null) observed.Add(before.R.State);
            var skipFirst = before.R != null;   // the state at the trigger sets the start, it is not checked
            long? firstChange = null;
            foreach (var (t, r) in inside)
            {
                if (observed.Count > 0 && observed[^1] == r.State) continue;
                if (observed.Count > 0 && firstChange == null) firstChange = t - w.T0;
                observed.Add(r.State);
            }
            var results = inside.Select(x => x.R.Result).Where(x => x != null).Distinct().ToList();
            var report = new CaseReport
            {
                File = file,
                Case = w.Case,
                Step = w.Step,
                Scored = w.Scored,
                Allowed = w.Allowed,
                Observed = observed,
                FirstChangeMs = firstChange,
                DetectMs = w.DetectMs,
                Results = results,
                ExpectResult = w.ExpectResult,
                DurationMs = w.T1 - w.T0,
            };
            if (w.Key != null)
            {
                var hit = inside.FirstOrDefault(x => x.R.State == w.Key);
                firstChange = hit.R == null ? null : hit.T - w.T0;
                report.FirstChangeMs = firstChange;
            }
            report.Why = Fits(skipFirst ? observed.Skip(1).ToList() : observed, w.Allowed);
            if (report.Why == null && w.DetectMs > 0 && w.Allowed.Count > 1 && (firstChange == null || firstChange > w.DetectMs))
                report.Why = (w.Key ?? "first change") + " at " + (firstChange?.ToString() ?? "never") + " ms, limit " + w.DetectMs;
            if (report.Why == null && w.ExpectResult != null && !w.ExpectResult.Split('|').Any(results.Contains))
                report.Why = "no result " + w.ExpectResult;
            report.Pass = report.Why == null;
            return report;
        }

        /// <summary>Null if the observed states follow the allowed list in order (entries can be skipped).</summary>
        static string Fits(List<string> observed, List<string> allowed)
        {
            var i = 0;
            foreach (var state in observed)
            {
                while (i < allowed.Count && !allowed[i].Split('|').Contains(state)) i++;
                if (i >= allowed.Count) return "state " + state + " is not allowed here";
            }
            return null;
        }

        static readonly System.Text.RegularExpressions.Regex RecordedBusyTitle = new(
            @"^(Hold on|Reloading|Importing|Running managed|Compiling|Refresh|Busy|Loading|Building)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Recordings made before the busy-window rule hold Unity's busy window in DialogTitle.
        /// They have no window structure, so this rule reads the English text of that time.
        /// Live samples use WindowSource.IsBusyWindow, which reads the structure.</summary>
        static void Normalize(Snapshot s)
        {
            if (s.DialogButtons == null || !RecordedBusyWindow(s.DialogTitle, s.DialogButtons)) return;
            s.BusyWindow = s.DialogTitle;
            s.BusySeconds = WindowSource.BusySeconds(s.DialogTitle);
            s.DialogTitle = null;
            s.DialogButtons = null;
        }

        static bool RecordedBusyWindow(string title, string buttons)
            => buttons.Contains("Skip Transcoding") || string.IsNullOrWhiteSpace(title) || RecordedBusyTitle.IsMatch(title.Trim());

        void Collect(string state, Snapshot s)
        {
            if (!Matrix.TryGetValue(state, out var signals)) Matrix[state] = signals = new();
            foreach (var f in Fields)
            {
                var v = f.GetValue(s);
                var text = v switch
                {
                    null => "null",
                    double d => Bucket(f.Name, d),
                    _ => v.ToString(),
                };
                if (!signals.TryGetValue(f.Name, out var set)) signals[f.Name] = set = new();
                if (set.Count < 6) set.Add(text);
            }
        }

        static string Bucket(string name, double d)
        {
            if (name.EndsWith("Pct")) return d < 5 ? "<5" : d < 50 ? "5-50" : ">50";
            if (name.EndsWith("AgeS")) return d < 3 ? "<3s" : d < 20 ? "3-20s" : ">20s";
            if (name.EndsWith("Ms")) return d < 100 ? "<100ms" : d < 2100 ? "<2.1s" : ">2.1s";
            return d.ToString("0");
        }
    }
}
