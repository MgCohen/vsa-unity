using System.Text.Json;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>Reads the state now. If a watch runs, its last sample has the full history.
    /// Otherwise one new sample gives the state with no history.</summary>
    public static class StatusReader
    {
        /// <summary>The name of the file in the status folder that names the running watch.</summary>
        public const string WatchMarker = "watch.json";

        /// <summary>A watch sample older than this is not current: the watch stopped or hangs.</summary>
        const double FreshSeconds = 5;

        public static async Task<StatusReport> Read(Target target, CancellationToken cancel = default)
        {
            var fromWatch = FromWatch(target);
            if (fromWatch != null) return fromWatch;
            var waiter = new StatusWaiter(target);
            var result = await waiter.Sample(cancel);
            return Report(result, waiter.LastSnapshot, target, history: false);
        }

        /// <summary>Writes watch.json: the PID of the watch process and its recording.</summary>
        public static void WriteMarker(Target target, string recording)
        {
            Directory.CreateDirectory(target.StatusFolder);
            File.WriteAllText(Path.Combine(target.StatusFolder, WatchMarker), JsonSerializer.Serialize(new
            {
                watchPid = Environment.ProcessId,
                recording,
            }));
        }

        public static void DeleteMarker(Target target)
        {
            var path = Path.Combine(target.StatusFolder, WatchMarker);
            if (File.Exists(path)) File.Delete(path);
        }

        static StatusReport FromWatch(Target target)
        {
            var marker = Path.Combine(target.StatusFolder, WatchMarker);
            if (!File.Exists(marker)) return null;
            var json = JsonFind.Parse(File.ReadAllText(marker));
            var recording = JsonFind.Str(json, "recording");
            if (JsonFind.Int(json, "watchPid") is not int pid || !Alive(pid) || recording == null || !File.Exists(recording)) return null;
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(recording) > TimeSpan.FromSeconds(FreshSeconds)) return null;
            var line = LastSample(recording);
            if (line == null) return null;
            using var doc = JsonDocument.Parse(line);
            var result = doc.RootElement.GetProperty("r").Deserialize<StateResult>(Recorder.Options);
            var snapshot = doc.RootElement.GetProperty("s").Deserialize<Snapshot>(Recorder.Options);
            EditorLauncher.TryLoadOwn(target);
            return Report(result, snapshot, target, history: true);
        }

        static StatusReport Report(StateResult result, Snapshot snapshot, Target target, bool history) => new()
        {
            State = result.State,
            Result = result.Result,
            Action = result.Action,
            AgeMs = result.AgeMs,
            SinceProgressMs = result.SinceProgressMs,
            History = history,
            Pid = target.Pid,
            EditorId = target.EditorId,
            RunFolder = target.RunFolder,
            Signals = snapshot,
        };

        static string LastSample(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            string last = null;
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                if (line.StartsWith("{\"kind\":\"sample\"", StringComparison.Ordinal)) last = line;
            return last;
        }

        static bool Alive(int pid)
        {
            try
            {
                using var p = System.Diagnostics.Process.GetProcessById(pid);
                return !p.HasExited;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}
