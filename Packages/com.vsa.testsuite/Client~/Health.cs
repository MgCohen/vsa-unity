using System.Net.Http;
using System.Text.Json;

namespace VSA.TestSuite.Client
{
    /// <summary>The health of an instance, as the supervisor sees it from outside.</summary>
    public enum HealthState
    {
        /// <summary>The server answers and the main thread is free.</summary>
        Ready,
        /// <summary>The server answers, but the Editor imports, compiles or reloads ("settling").</summary>
        Busy,
        /// <summary>A modal dialog blocks the main thread.</summary>
        Dialog,
        /// <summary>The main thread does not answer in time, and no dialog is open. A long synchronous command also gives this state.</summary>
        Frozen,
        /// <summary>The process runs, but no server listens: start, domain reload, or Safe Mode.</summary>
        Unreachable,
        /// <summary>The process does not run.</summary>
        Dead,
    }

    public sealed class HealthReport
    {
        public HealthState State;
        public string Status;
        public string Detail;
        public double Ms;
        public bool? PlayerLoopTicking;
        public override string ToString() => State + (Detail == null ? "" : " (" + Detail + ")");
    }

    /// <summary>
    /// Classifies the health of an instance with short probes. A probe never waits for the command queue:
    /// /api/status and /api/dialog are served off the main thread.
    /// </summary>
    public sealed class Health
    {
        readonly ITransport transport;

        public int ProbeTimeoutMs { get; set; } = 2000;

        public Health(ITransport transport) => this.transport = transport;

        public async Task<HealthReport> Probe(CancellationToken cancel = default)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var instance = transport.Instance;
            bool loaded = instance.TryLoad();
            if (loaded && !instance.ProcessAlive())
                return Done(new HealthReport { State = HealthState.Dead, Detail = "process " + instance.Pid + " does not run" }, watch);
            if (!loaded)
                return Done(new HealthReport { State = HealthState.Unreachable, Detail = "no descriptor" }, watch);

            JsonElement? status;
            try
            {
                status = await transport.Get("/api/status", ProbeTimeoutMs, cancel);
            }
            catch (HttpRequestException)
            {
                return Done(new HealthReport { State = instance.ProcessAlive() ? HealthState.Unreachable : HealthState.Dead, Detail = "no server on port " + instance.Port }, watch);
            }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
            {
                return Done(await AfterTimeout(cancel), watch);
            }

            var report = new HealthReport();
            if (status is JsonElement s)
            {
                report.Status = s.TryGetProperty("status", out var st) ? st.GetString() : null;
                if (s.TryGetProperty("playerLoopTicking", out var tick)) report.PlayerLoopTicking = tick.ValueKind == JsonValueKind.True;
                if (report.Status == "blocked_by_dialog" && s.TryGetProperty("dialog", out var dlg))
                    report.Detail = dlg.TryGetProperty("title", out var title) ? title.GetString() : dlg.ToString();
            }
            report.State = report.Status switch
            {
                "ready" => HealthState.Ready,
                "blocked_by_dialog" => HealthState.Dialog,
                _ => HealthState.Busy,
            };
            if (report.State == HealthState.Busy) report.Detail ??= report.Status;
            if (report.State != HealthState.Ready) return Done(report, watch);

            // /api/status is cached off the main thread: it says "ready" also when the main thread
            // is frozen. /api/editor_status needs the main thread, so it is the real probe.
            try
            {
                await transport.Get("/api/editor_status", ProbeTimeoutMs, cancel);
            }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
            {
                return Done(await AfterTimeout(cancel), watch);
            }
            catch (HttpRequestException)
            {
                return Done(new HealthReport { State = HealthState.Unreachable, Detail = "the server stopped during the probe" }, watch);
            }
            return Done(report, watch);
        }

        async Task<HealthReport> AfterTimeout(CancellationToken cancel)
        {
            bool commandRunning = false;
            try
            {
                var dialog = await transport.Get("/api/dialog", ProbeTimeoutMs, cancel);
                if (dialog is JsonElement d && d.TryGetProperty("active", out var active) && active.ValueKind == JsonValueKind.True)
                    return new HealthReport { State = HealthState.Dialog, Detail = d.ToString() };
                var progress = await transport.Get("/api/progress", ProbeTimeoutMs, cancel);
                commandRunning = progress is JsonElement p && p.TryGetProperty("active", out var a) && a.ValueKind == JsonValueKind.True;
            }
            catch (Exception e) when (e is HttpRequestException || e is OperationCanceledException) { }
            return new HealthReport
            {
                State = HealthState.Frozen,
                Detail = "the main thread gave no answer in " + ProbeTimeoutMs + " ms; "
                    + (commandRunning ? "a command is running" : "no command is running"),
            };
        }

        static HealthReport Done(HealthReport r, System.Diagnostics.Stopwatch w)
        {
            r.Ms = Math.Round(w.Elapsed.TotalMilliseconds, 1);
            return r;
        }

        /// <summary>Waits until the instance is Ready. Fails at once on Dialog or Dead.</summary>
        public async Task<HealthReport> WaitReady(int timeoutMs, CancellationToken cancel = default)
        {
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (true)
            {
                var r = await Probe(cancel);
                if (r.State == HealthState.Ready) return r;
                if (r.State == HealthState.Dialog || r.State == HealthState.Dead || DateTime.UtcNow > until)
                    throw new InstanceException(transport.Instance, "The instance is not ready: " + r);
                await Task.Delay(300, cancel);
            }
        }
    }
}
