using System.Diagnostics;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>Runs one slow "unity" CLI call in the background and keeps its last output. One call at a time.</summary>
    public sealed class CliPoller
    {
        readonly string arguments;
        Task running;
        DateTime lastAt;

        public CliPoller(string arguments) => this.arguments = arguments;

        public string Last { get; private set; }

        public long AgeMs => lastAt == default ? -1 : (long)(DateTime.UtcNow - lastAt).TotalMilliseconds;

        /// <summary>Starts a new call if none runs and the last one is older than 6 s.</summary>
        public void Kick(Target target)
        {
            if (target.ProjectPath == null || running is { IsCompleted: false }) return;
            if (lastAt != default && DateTime.UtcNow - lastAt < TimeSpan.FromSeconds(6)) return;
            running = Task.Run(() => Run(target.ProjectPath));
        }

        async Task Run(string projectPath)
        {
            var info = new ProcessStartInfo("unity", arguments + " --no-banner")
            {
                WorkingDirectory = projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            try
            {
                using var p = Process.Start(info);
                var output = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                Last = output;
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                Last = null;
            }
            lastAt = DateTime.UtcNow;
        }
    }
}
