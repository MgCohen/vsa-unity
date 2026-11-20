namespace VSA.TestSuite.Client.Status
{
    /// <summary>The only writer of Memory. Moves the clocks, classifies each snapshot,
    /// and adds the results stalled, stuck and loop.</summary>
    public sealed class StatusTracker
    {
        static readonly HashSet<string> DialogStates = new() { "safe-mode-prompt", "backup-prompt", "native-dialog" };

        readonly IClock clock;
        readonly ActionLog actions;
        readonly Limits limits;
        int consumed;
        string loopReason;

        public Memory Memory { get; } = new();

        public StatusTracker(IClock clock, ActionLog actions, Limits limits)
        {
            this.clock = clock;
            this.actions = actions;
            this.limits = limits;
        }

        public StateResult Observe(Snapshot now)
        {
            Consume(now.T);
            var m = Memory;
            var t = clock.NowMs;
            var state = StatusChecker.Check(now, m);

            if (state.Name != m.LastState)
            {
                m.LastState = state.Name;
                m.StateSinceMs = t;
                m.LastProgressMs = t;
                if (DialogStates.Contains(state.Name)) Count(m.DialogCounts, state.Name + "|" + now.DialogTitle, 3, "dialog " + now.DialogTitle);
            }
            if (Progressed(now, state.Name)) m.LastProgressMs = t;
            Remember(now);
            if (now.ProbeOk && now.ServerUp(m)) m.EverReady = true;

            var result = new StateResult
            {
                State = state.Name,
                Action = state.Action,
                AgeMs = t - m.StateSinceMs,
                SinceProgressMs = t - m.LastProgressMs,
            };
            if (limits.TryGet(state.Name, m.FullImport, out var capMs, out var stallMs))
            {
                if (capMs > 0 && result.AgeMs > capMs) result.Result = "stuck";
                else if (result.SinceProgressMs > stallMs) result.Result = "stalled";
            }
            if (loopReason != null)
            {
                result.Result = "loop";
                result.Evidence = loopReason;
                loopReason = null;
            }
            if (result.Result is "stuck" or "stalled") result.Action = StateAction.Escalate;
            if (result.Result == "loop") result.Action = StateAction.Fail;
            return result;
        }

        bool Progressed(Snapshot now, string state)
        {
            var m = Memory;
            if (now.LogSize > m.LogSize) return true;
            if (now.ActivitySize > m.ActivitySize) return true;
            if (now.ProgressText != null && now.ProgressText != m.ProgressText) return true;
            if (now.BeaconOwn && now.BeaconSeq != m.BeaconSeq) return true;
            // Unity's progress bars and step text: the startup window and the in-Editor busy window.
            if (now.SplashBar != null && now.SplashBar != m.SplashBar) return true;
            if (now.BusyBar != null && now.BusyBar != m.BusyBar) return true;
            if (now.BusyStep != null && now.BusyStep != m.BusyStep) return true;
            // CPU time is no progress signal: Unity background threads use CPU also during a hang (measured).
            return false;
        }

        void Remember(Snapshot now)
        {
            var m = Memory;
            m.LogSize = Math.Max(m.LogSize, now.LogSize);
            m.ActivitySize = Math.Max(m.ActivitySize, now.ActivitySize);
            m.ProgressText = now.ProgressText ?? m.ProgressText;
            if (now.BeaconOwn) m.BeaconSeq = now.BeaconSeq;
            m.CpuMs = now.ProcCpuMs;
            m.SplashBar = now.SplashBar ?? m.SplashBar;
            m.BusyBar = now.BusyBar ?? m.BusyBar;
            m.BusyStep = now.BusyStep ?? m.BusyStep;
        }

        /// <summary>Applies the actions that happened before the sample time. A later action belongs to a later sample.</summary>
        void Consume(long sampleT)
        {
            var m = Memory;
            for (; consumed < actions.Entries.Count; consumed++)
            {
                var e = actions.Entries[consumed];
                if (e.T > sampleT) break;
                switch (e.Kind)
                {
                    case "own-pid": m.OwnPid = int.Parse(e.Detail); break;
                    case "attach": m.Attached = true; break;
                    case "forget": m.ForgetEditor(); m.LogSize = 0; m.BeaconSeq = 0; break;
                    case "full-import": m.FullImport = true; break;
                    case "play-command": m.LastPlayCommandMs = e.T; break;
                    case "job-start": m.JobActive = true; break;
                    case "job-end": m.JobActive = false; break;
                    case "recovery": m.Recoveries++; break;
                    case "escalate-stop": case "escalate-close": case "escalate-quit": case "stop": m.StopSent = true; break;
                    case "recovery-reset": m.Recoveries = 0; m.StepFailures.Clear(); break;
                    case "step-result": Count(m.StepFailures, e.Detail, 2, "step " + e.Detail); break;
                }
            }
        }

        void Count(Dictionary<string, int> counts, string key, int limit, string reason)
        {
            counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
            if (counts[key] >= limit) loopReason = reason + " x" + counts[key];
        }
    }
}
