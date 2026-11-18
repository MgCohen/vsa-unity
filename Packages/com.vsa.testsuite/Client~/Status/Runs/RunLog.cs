using System.Globalization;
using System.Text.Json;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>The run folders in the log root: list, read, mark and prune. Each Editor start has its own folder
    /// &lt;root&gt;/&lt;editorId&gt;/ with unity.log, run.json and, if a daemon ran, status.ndjson.</summary>
    public static class RunLog
    {
        public const string RunFile = "run.json";
        public const string StatusFile = "status.ndjson";
        public const string UnityLogFile = "unity.log";
        const string PrecheckPrefix = "precheck-";

        /// <summary>All run folders, newest first.</summary>
        public static List<RunInfo> Sessions(string root)
        {
            if (root == null || !Directory.Exists(root)) return new List<RunInfo>();
            return Directory.GetDirectories(root).Select(Info)
                .OrderByDescending(r => r.StartedUtc ?? DateTime.MinValue)
                .ThenByDescending(r => r.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The last <paramref name="tail"/> lines of unity.log and status.ndjson of one run.
        /// A null id means the newest Editor run.</summary>
        public static RunLines Show(string root, string runId, int tail)
        {
            var run = Find(root, runId);
            return new RunLines
            {
                Run = run,
                UnityLog = Tail(run.UnityLog, tail),
                StatusLog = Tail(run.StatusLog, tail),
            };
        }

        /// <summary>Appends a mark line to status.ndjson of a run. A null id means the newest Editor run.
        /// Returns the file path.</summary>
        public static string Mark(string root, string runId, string text)
        {
            var run = Find(root, runId);
            var path = Path.Combine(run.Folder, StatusFile);
            var line = JsonSerializer.Serialize(new
            {
                kind = "mark",
                utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                text,
            });
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
            return path;
        }

        /// <summary>Deletes the oldest run folders until <paramref name="keep"/> are left.
        /// The folder of a running Editor stays.</summary>
        public static void Prune(string root, int keep)
        {
            if (root == null || !Directory.Exists(root)) return;
            foreach (var run in Sessions(root).Skip(Math.Max(0, keep)))
            {
                if (run.Running) continue;
                try { Directory.Delete(run.Folder, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        static RunInfo Find(string root, string runId)
        {
            var runs = Sessions(root);
            var run = runId == null ? runs.FirstOrDefault(r => r.Kind == "editor") : runs.FirstOrDefault(r => r.Id == runId);
            if (run != null) return run;
            throw new FileNotFoundException(runId == null ? "No Editor run in " + root + "." : "No run '" + runId + "' in " + root + ".");
        }

        static RunInfo Info(string folder)
        {
            var id = Path.GetFileName(folder);
            var run = new RunInfo
            {
                Id = id,
                Kind = id.StartsWith(PrecheckPrefix, StringComparison.Ordinal) ? "precheck" : "editor",
                Folder = folder,
                UnityLog = Path.Combine(folder, UnityLogFile),
            };
            var stamp = run.Kind == "precheck" ? id.Substring(PrecheckPrefix.Length) : id;
            if (stamp.Length >= 15 && DateTime.TryParseExact(stamp.Substring(0, 15), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
                run.StartedUtc = at;
            run.UnityLogBytes = File.Exists(run.UnityLog) ? new FileInfo(run.UnityLog).Length : 0;
            var statusLog = Path.Combine(folder, StatusFile);
            run.StatusLog = File.Exists(statusLog) ? statusLog : null;
            var file = Path.Combine(folder, RunFile);
            if (!File.Exists(file)) return run;
            try
            {
                run.Pid = JsonFind.Int(JsonFind.Parse(File.ReadAllText(file)), "pid");
                run.Running = run.Pid is int pid && IsEditor(pid);
            }
            catch (IOException) { }
            return run;
        }

        /// <summary>True if the PID is a running Unity Editor.</summary>
        static bool IsEditor(int pid)
        {
            try
            {
                using var p = System.Diagnostics.Process.GetProcessById(pid);
                return !p.HasExited && p.ProcessName.Equals("Unity", StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        static List<string> Tail(string path, int count)
        {
            if (path == null || !File.Exists(path) || count <= 0) return new List<string>();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var lines = new Queue<string>();
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                lines.Enqueue(line);
                if (lines.Count > count) lines.Dequeue();
            }
            return lines.ToList();
        }
    }
}
