using System.Globalization;
using System.Text.Json;

namespace StatusLab
{
    /// <summary>desc: the Pipeline descriptor file. Stores the port and token in the target only; the snapshot never holds the token.</summary>
    public sealed class DescriptorSource : ISignalSource
    {
        public string Key => "desc";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            // Keep the last port while the file is read: a playbook trigger uses it at the same time.
            if (!File.Exists(target.DescriptorPath)) { target.Port = 0; return Task.CompletedTask; }
            JsonElement root;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(target.DescriptorPath));
                root = doc.RootElement.Clone();
            }
            catch (Exception e) when (e is IOException or JsonException)
            {
                return Task.CompletedTask;   // The Editor writes the file now. The next sample reads it.
            }
            into.DescPresent = true;
            into.DescPid = JsonFind.Int(root, "pid") ?? 0;
            into.DescPidAlive = Alive(into.DescPid);
            var info = JsonFind.Find(root, "info");
            into.DescInfoSet = info is JsonElement i && i.ValueKind != JsonValueKind.Null
                && !(i.ValueKind == JsonValueKind.String && i.GetString().Length == 0)
                && !(i.ValueKind == JsonValueKind.Object && !i.EnumerateObject().Any());
            var beat = JsonFind.Str(root, "lastHeartbeat");
            if (DateTime.TryParse(beat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
                into.DescHeartbeatAgeS = Math.Round((DateTime.UtcNow - at).TotalSeconds, 1);
            target.Port = JsonFind.Int(root, "port") ?? 0;
            target.Token = JsonFind.Str(root, "evalToken");
            return Task.CompletedTask;
        }

        static bool Alive(int pid)
        {
            if (pid <= 0) return false;
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
