namespace VSA.StatusChecker
{
    /// <summary>One live recording: samples every 500 ms, classifies, records, and lets the supervisor act.</summary>
    public sealed class LiveSession : IDisposable
    {
        public const int IntervalMs = 500;

        public Target Target { get; }
        public WallClock Clock { get; } = new();
        public ActionLog Actions { get; }
        public StatusTracker Tracker { get; }
        public Sampler Sampler { get; }
        public Supervisor Supervisor { get; }
        public Recorder Recorder { get; }
        public IEditorControl Control { get; }

        public StateResult Last { get; private set; }
        public Snapshot LastSnapshot { get; private set; }

        public LiveSession(Target target, string recordPath, IEditorControl control = null)
        {
            Target = target;
            Actions = new ActionLog(Clock);
            Tracker = new StatusTracker(Clock, Actions, Limits.Load());
            Sampler = new Sampler(Clock);
            Control = control ?? new EditorControl();
            Supervisor = new Supervisor(Control, Actions, Clock);
            Recorder = new Recorder(recordPath);
            Actions.Added += Recorder.Action;
            if (target.Pid > 0) Actions.Add("playbook", "own-pid", target.Pid.ToString());
        }

        public async Task Run(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                var started = Clock.NowMs;
                var snapshot = await Sampler.Read(Target, cancel);
                var result = Tracker.Observe(snapshot);
                Recorder.Sample(snapshot, result);
                LastSnapshot = snapshot;
                Last = result;
                await Supervisor.Step(result, snapshot, Tracker.Memory, Target, cancel);
                var wait = IntervalMs - (int)(Clock.NowMs - started);
                if (wait > 0) await Task.Delay(wait, cancel);
            }
        }

        /// <summary>Waits until the last result is one of the states, or the time ends. Returns the last result.</summary>
        public async Task<StateResult> WaitFor(Func<StateResult, bool> done, int timeoutMs, CancellationToken cancel)
        {
            var until = Clock.NowMs + timeoutMs;
            while (Clock.NowMs < until)
            {
                if (Last != null && done(Last)) return Last;
                await Task.Delay(250, cancel);
            }
            return Last;
        }

        public void Dispose() => Recorder.Dispose();
    }
}
