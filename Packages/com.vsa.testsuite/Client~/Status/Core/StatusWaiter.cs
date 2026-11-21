namespace VSA.TestSuite.Client.Status
{
    /// <summary>Samples the own Editor, classifies each sample, and waits for a state. It reads only:
    /// it has no supervisor. Each wait has a time limit.</summary>
    public sealed class StatusWaiter
    {
        readonly Target target;
        readonly Sampler sampler;

        public StatusWaiter(Target target, Limits limits = null, IClock clock = null)
        {
            this.target = target;
            Clock = clock ?? new WallClock();
            Actions = new ActionLog(Clock);
            Tracker = new StatusTracker(Clock, Actions, limits ?? Limits.Load());
            sampler = new Sampler(Clock);
            if (target.Pid > 0) Actions.Add("waiter", "own-pid", target.Pid.ToString());
        }

        public IClock Clock { get; }
        public ActionLog Actions { get; }
        public StatusTracker Tracker { get; }

        /// <summary>The time between two samples.</summary>
        public int IntervalMs { get; set; } = 500;

        /// <summary>Attach mode: while no own PID is known, use the live PID of the descriptor.
        /// For an Editor or a player that another tool started. Read-only use only.</summary>
        public bool AdoptDescriptorPid { get; set; }

        /// <summary>Called after each sample.</summary>
        public Action<StateResult, Snapshot> Observed { get; set; }

        public StateResult Last { get; private set; }
        public Snapshot LastSnapshot { get; private set; }

        /// <summary>Takes one sample and classifies it.</summary>
        public async Task<StateResult> Sample(CancellationToken cancel = default)
        {
            if (AdoptDescriptorPid && target.Pid <= 0) Adopt();
            var snapshot = await sampler.Read(target, cancel);
            var result = Tracker.Observe(snapshot);
            Last = result;
            LastSnapshot = snapshot;
            Observed?.Invoke(result, snapshot);
            return result;
        }

        /// <summary>Waits until the state is one of <paramref name="until"/>, or a stop rule of WaitRules ends the wait,
        /// or the time limit ends it with the code "Timeout".</summary>
        public async Task<WaitOutcome> Wait(ISet<string> until, long timeoutMs, CancellationToken cancel = default)
        {
            if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs), "A wait needs a time limit.");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                var started = watch.ElapsedMilliseconds;
                var result = await Sample(cancel);
                if (until.Contains(result.State) && result.Result == null)
                    return Done(true, null, null, watch);
                var code = WaitRules.StopCode(result, editorRequired: !AdoptDescriptorPid);
                if (code != null) return Done(false, code, result + (result.Evidence == null ? "" : ": " + result.Evidence), watch);
                if (watch.ElapsedMilliseconds >= timeoutMs)
                    return Done(false, "Timeout", "The state is " + result + " after " + timeoutMs / 1000 + " s.", watch);
                var wait = IntervalMs - (int)(watch.ElapsedMilliseconds - started);
                if (wait > 0) await Task.Delay(wait, cancel);
            }
        }

        WaitOutcome Done(bool met, string code, string message, System.Diagnostics.Stopwatch watch) => new()
        {
            Met = met,
            Code = code,
            Message = message,
            Result = Last,
            Snapshot = LastSnapshot,
            ElapsedMs = watch.ElapsedMilliseconds,
        };

        void Adopt()
        {
            if (!File.Exists(target.DescriptorPath)) return;
            int pid;
            try { pid = JsonFind.Int(JsonFind.Parse(File.ReadAllText(target.DescriptorPath)), "pid") ?? 0; }
            catch (IOException) { return; }
            if (pid <= 0) return;
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(pid);
                if (process.HasExited) return;
                target.Pid = pid;
                target.Process = process;
                Actions.Add("waiter", "attach");
                Actions.Add("waiter", "own-pid", pid.ToString());
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
    }
}
