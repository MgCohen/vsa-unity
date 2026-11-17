using System.Diagnostics;
using System.Text.Json;

namespace StatusLab
{
    /// <summary>Starts the own Editor with "unity open", then finds its Unity.exe by the unique -logFile path
    /// in the command line, and opens a process handle at once so the exit code stays readable.</summary>
    public static class EditorLauncher
    {
        public static async Task Start(Target target, CancellationToken cancel)
        {
            var foreign = FindUnity(target.ProjectPath);
            if (foreign.Count > 0)
                throw new InvalidOperationException("An Editor already runs on this project: PID " + string.Join(", ", foreign.Select(f => f.Pid)));

            target.EditorId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            target.LogPath = Path.Combine(target.ProjectPath, "Logs", "vsa-watch", target.EditorId, "unity.log");
            Directory.CreateDirectory(Path.GetDirectoryName(target.LogPath));
            target.StartedUtc = DateTime.UtcNow;
            target.Pid = 0;
            target.Process = null;

            var info = new ProcessStartInfo("unity")
            {
                WorkingDirectory = target.ProjectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var a in new[] { "open", ".", "--args", "-automated -statuslab " + (target.ExtraArgs == null ? "" : target.ExtraArgs + " ") + "-logFile " + target.LogPath, "--until-ready", "--timeout", "600", "--no-banner" })
                info.ArgumentList.Add(a);
            info.Environment["UNITY_DONOTSTARTBUGREPORTER"] = "1";
            var cli = Process.Start(info);
            _ = cli.StandardOutput.ReadToEndAsync();
            _ = cli.StandardError.ReadToEndAsync();

            var until = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < until)
            {
                cancel.ThrowIfCancellationRequested();
                var own = FindUnity(target.ProjectPath).FirstOrDefault(f => f.CommandLine.Contains(target.LogPath, StringComparison.OrdinalIgnoreCase));
                if (own.Pid > 0)
                {
                    target.Pid = own.Pid;
                    target.Process = Process.GetProcessById(own.Pid);
                    _ = target.Process.Handle;   // open the handle now
                    SaveOwn(target);
                    return;
                }
                await Task.Delay(500, cancel);
            }
            throw new TimeoutException("No Unity.exe with the log path " + target.LogPath + " in 90 s");
        }

        /// <summary>Compiles the project in batch mode before a start. Batch mode has no Safe Mode: with compile errors
        /// it exits with a non-zero code. Runs only when no Editor has the project open.</summary>
        public static async Task<(int ExitCode, long Ms, List<string> Errors, string LogPath)> Precheck(Target target, CancellationToken cancel)
        {
            if (FindUnity(target.ProjectPath).Count > 0) throw new InvalidOperationException("An Editor has the project open.");
            var version = File.ReadLines(Path.Combine(target.ProjectPath, "ProjectSettings", "ProjectVersion.txt"))
                .First(l => l.StartsWith("m_EditorVersion:")).Split(':')[1].Trim();
            var exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor", version, "Editor", "Unity.exe");
            var log = Path.Combine(target.ProjectPath, "Logs", "vsa-watch", "precheck-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), "unity.log");
            Directory.CreateDirectory(Path.GetDirectoryName(log));
            var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var a in new[] { "-batchmode", "-quit", "-nographics", "-projectPath", target.ProjectPath, "-logFile", log }) info.ArgumentList.Add(a);
            info.Environment["UNITY_DONOTSTARTBUGREPORTER"] = "1";
            var watch = Stopwatch.StartNew();
            using var p = Process.Start(info);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(TimeSpan.FromMinutes(15));
            try { await p.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested) { p.Kill(); }
            var errors = new List<string>();
            for (var attempt = 0; attempt < 20 && File.Exists(log); attempt++)
            {
                try
                {
                    // Unity can hold the log for a moment after its exit, so read with shared access.
                    using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    errors = reader.ReadToEnd().Split((char)10).Select(l => l.Trim()).Where(l => l.Contains("error CS") || l.Contains("error occurred while resolving packages") || l.Contains("cannot be found")).Distinct().Take(20).ToList();
                    break;
                }
                catch (IOException) { await Task.Delay(250, cancel); }
            }
            return (p.HasExited ? p.ExitCode : -1, watch.ElapsedMilliseconds, errors, log);
        }

        /// <summary>Unity Editor processes whose command line names this project. The unity CLI is also named
        /// unity.exe, and asset import workers are Unity.exe with -projectPath: both are left out.</summary>
        public static List<(int Pid, string CommandLine)> FindUnity(string projectPath)
        {
            var info = new ProcessStartInfo("powershell", "-NoProfile -NonInteractive -Command \"Get-CimInstance Win32_Process -Filter \\\"Name='Unity.exe'\\\" | Where-Object { $_.ExecutablePath -like '*\\Editor\\Unity.exe' -and $_.CommandLine -notlike '*AssetImportWorker*' } | Select-Object ProcessId,CommandLine | ConvertTo-Json -Compress\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(info);
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            var list = new List<(int, string)>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            using var doc = JsonDocument.Parse(text);
            var items = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToList() : new List<JsonElement> { doc.RootElement };
            var project = Path.GetFullPath(projectPath).TrimEnd('\\', '/');
            foreach (var item in items)
            {
                var line = item.GetProperty("CommandLine").GetString() ?? "";
                if (line.Contains(project, StringComparison.OrdinalIgnoreCase) || line.Contains(project.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    list.Add((item.GetProperty("ProcessId").GetInt32(), line));
            }
            return list;
        }

        /// <summary>Library/StatusLab/own-editor.json: lets a later spike command find the own Editor.</summary>
        static void SaveOwn(Target target)
        {
            Directory.CreateDirectory(target.LabFolder);
            File.WriteAllText(Path.Combine(target.LabFolder, "own-editor.json"), JsonSerializer.Serialize(new
            {
                pid = target.Pid,
                logPath = target.LogPath,
                editorId = target.EditorId,
                startedUtc = target.StartedUtc,
            }));
        }

        /// <summary>Loads the own Editor from own-editor.json. Returns false if it does not run.</summary>
        public static bool TryLoadOwn(Target target)
        {
            var path = Path.Combine(target.LabFolder, "own-editor.json");
            if (!File.Exists(path)) return false;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            target.LogPath = doc.RootElement.GetProperty("logPath").GetString();
            target.EditorId = doc.RootElement.GetProperty("editorId").GetString();
            target.StartedUtc = doc.RootElement.GetProperty("startedUtc").GetDateTime();
            // Find the PID again by the unique log path, so a recycled PID never counts.
            var own = FindUnity(target.ProjectPath).FirstOrDefault(f => f.CommandLine.Contains(target.LogPath, StringComparison.OrdinalIgnoreCase));
            if (own.Pid <= 0) return false;
            target.Pid = own.Pid;
            target.Process = Process.GetProcessById(own.Pid);
            _ = target.Process.Handle;
            SaveOwn(target);
            return true;
        }
    }
}
