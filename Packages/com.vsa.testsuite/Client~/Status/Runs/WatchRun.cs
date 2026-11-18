namespace VSA.TestSuite.Client.Status
{
    /// <summary>The supervisor loop of "watch": it starts the own Editor if none runs (after the batch-mode compile check),
    /// samples it, records each sample to watch.ndjson, and lets the supervisor act. It never acts on an Editor
    /// that it did not start.</summary>
    public sealed class WatchRun
    {
        readonly Target target;
        readonly IEditorControl control;

        public WatchRun(Target target, IEditorControl control = null)
        {
            this.target = target;
            this.control = control ?? new EditorControl();
        }

        /// <summary>Live acts; DryRun only writes what it would do.</summary>
        public SupervisorMode Mode { get; set; } = SupervisorMode.Live;

        /// <summary>Called for each change of the state or result, and for each action.</summary>
        public Action<object> Line { get; set; }

        /// <summary>Runs until the Editor exits, the supervisor gives up, the time limit ends, or the caller cancels.</summary>
        public async Task<WaitOutcome> Run(long timeoutMs, CancellationToken cancel = default)
        {
            if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs), "A watch needs a time limit.");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (target.Pid <= 0 && !EditorLauncher.TryLoadOwn(target))
            {
                try
                {
                    await control.Start(target, cancel);
                }
                catch (StartRefusedException e)
                {
                    return new WaitOutcome
                    {
                        Code = "StartRefused",
                        Message = e.Message + (e.Errors.Count == 0 ? "" : " " + string.Join(" | ", e.Errors)) + (e.LogPath == null ? "" : " Log: " + e.LogPath),
                        ElapsedMs = watch.ElapsedMilliseconds,
                    };
                }
                Line?.Invoke(new { kind = "start", pid = target.Pid, editorId = target.EditorId, log = target.LogPath });
            }

            var recording = Path.Combine(target.RunFolder, RunLog.WatchFile);
            using var session = new LiveSession(target, recording, control);
            session.Supervisor.Mode = Mode;
            session.Supervisor.RestartOnCrash = true;
            session.Actions.Added += a => Line?.Invoke(new { kind = "action", a.Who, a.Kind, a.Detail });
            StatusReader.WriteMarker(target, recording);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            var loop = session.Run(stop.Token);
            string last = null;
            WaitOutcome outcome = null;
            try
            {
                while (outcome == null)
                {
                    await Task.WhenAny(loop, Task.Delay(250, cancel));
                    if (loop.IsFaulted) await loop;
                    var r = session.Last;
                    if (r != null && r.ToString() != last)
                    {
                        last = r.ToString();
                        Line?.Invoke(new { kind = "state", r.State, r.Result, r.Action, r.AgeMs, r.SinceProgressMs, pid = target.Pid });
                    }
                    if (session.Supervisor.Failed)
                        outcome = End("SupervisorFailed", session.Actions.Entries.LastOrDefault(a => a.Kind == "fail-run")?.Detail, session, watch);
                    else if (r?.State == "exited")
                        outcome = End(null, "The Editor exited.", session, watch);
                    else if (watch.ElapsedMilliseconds >= timeoutMs)
                        outcome = End("Timeout", "The watch reached its time limit of " + timeoutMs / 1000 + " s.", session, watch);
                    else if (cancel.IsCancellationRequested)
                        outcome = End("Cancelled", "The watch was cancelled.", session, watch);
                }
            }
            finally
            {
                stop.Cancel();
                try { await loop; }
                catch (OperationCanceledException) { }
                StatusReader.DeleteMarker(target);
            }
            return outcome;
        }

        static WaitOutcome End(string code, string message, LiveSession session, System.Diagnostics.Stopwatch watch) => new()
        {
            Met = code == null,
            Code = code,
            Message = message,
            Result = session.Last,
            Snapshot = session.LastSnapshot,
            ElapsedMs = watch.ElapsedMilliseconds,
        };
    }
}
