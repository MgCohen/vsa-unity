namespace VSA.TestSuite.Client.Status
{
    /// <summary>proc: the own PID is alive, its CPU use, and its exit code after the exit.</summary>
    public sealed class ProcessSource : ISignalSource
    {
        double lastCpuMs;
        DateTime lastAt;

        public string Key => "proc";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            var p = target.Process;
            if (p == null) return Task.CompletedTask;
            p.Refresh();
            if (p.HasExited)
            {
                into.ProcExitCode = p.ExitCode;
                return Task.CompletedTask;
            }
            into.ProcAlive = true;
            var now = DateTime.UtcNow;
            var cpu = p.TotalProcessorTime.TotalMilliseconds;
            if (lastAt != default)
            {
                var wall = (now - lastAt).TotalMilliseconds;
                if (wall > 0) into.ProcCpuPct = Math.Round((cpu - lastCpuMs) / wall / Environment.ProcessorCount * 100, 1);
            }
            into.ProcCpuMs = cpu;
            lastCpuMs = cpu;
            lastAt = now;
            return Task.CompletedTask;
        }
    }
}
