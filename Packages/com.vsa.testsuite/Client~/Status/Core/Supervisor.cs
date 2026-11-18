namespace VSA.TestSuite.Client.Status
{
    /// <summary>Acts on each result through IEditorControl: answers dialogs and escalates stalled or stuck states.
    /// Each escalation step runs only if the step before it did not work in 15 s.</summary>
    public sealed class Supervisor
    {
        const long StepGapMs = 15_000;

        readonly IEditorControl control;
        readonly ActionLog actions;
        readonly IClock clock;
        int stage = -1;
        long stageAt;
        bool dryLogged;

        public Supervisor(IEditorControl control, ActionLog actions, IClock clock)
        {
            this.control = control;
            this.actions = actions;
            this.clock = clock;
        }

        public SupervisorMode Mode { get; set; } = SupervisorMode.Off;

        /// <summary>True after the supervisor gave up: the caller must end the run.</summary>
        public bool Failed { get; private set; }

        /// <summary>Starts a new case: clears Failed and the escalation stage.</summary>
        public void Reset()
        {
            Failed = false;
            stage = -1;
            dryLogged = false;
        }

        /// <summary>True: after a crash, when the process is gone, start the Editor again (one recovery only).
        /// The lab playbooks leave it false, because they start the Editor themselves.</summary>
        public bool RestartOnCrash { get; set; }

        /// <summary>Runs before a recovery start, for example to delete hang.flag.</summary>
        public Func<Task> BeforeRestart { get; set; }

        public async Task Step(StateResult result, Snapshot now, Memory memory, Target target, CancellationToken cancel)
        {
            if (Mode == SupervisorMode.Off || Failed) return;
            if (stage >= 0 && Mode == SupervisorMode.Live && !now.ProcAlive)
            {
                // An escalation step ended the process: go to the recovery start.
                stage = 3;
                stageAt = clock.NowMs;
                await Escalate(stage, result, now, memory, target, cancel);
                return;
            }
            if (RestartOnCrash && Mode == SupervisorMode.Live && result.State == "crash" && !now.ProcAlive && memory.OwnPid != 0)
            {
                actions.Add("supervisor", "escalate-evidence", "crash, exit code " + now.ProcExitCode);
                stage = 3;
                stageAt = clock.NowMs;
                await Escalate(stage, result, now, memory, target, cancel);
                return;
            }
            if (result.Action == StateAction.Answer && result.Result == null)
            {
                Answer(result.State, now, target);
                return;
            }
            if (result.State == "safe-mode" && Mode == SupervisorMode.Live)
            {
                // Never restart: the code still has errors, so a new start enters Safe Mode again.
                actions.Add("supervisor", "close-safe-mode");
                control.CloseWindow(target);
                actions.Add("supervisor", "fail-run", "Safe Mode: compile errors at start");
                Failed = true;
                return;
            }
            if (result.Action != StateAction.Escalate)
            {
                if (now.ProbeOk) { stage = -1; dryLogged = false; }
                return;
            }
            if (Mode == SupervisorMode.DryRun)
            {
                if (!dryLogged) actions.Add("supervisor", "escalate-dry", result.ToString());
                dryLogged = true;
                return;
            }
            if (stage >= 0 && clock.NowMs - stageAt < StepGapMs) return;
            stage++;
            stageAt = clock.NowMs;
            await Escalate(stage, result, now, memory, target, cancel);
        }

        void Answer(string state, Snapshot now, Target target)
        {
            var button = state switch
            {
                "safe-mode-prompt" => "Ignore",
                "backup-prompt" => "Delete",
                _ => "OK",
            };
            if (Mode == SupervisorMode.DryRun)
            {
                actions.Add("supervisor", "answer-dry", now.DialogTitle + " -> " + button);
                return;
            }
            var done = control.AnswerDialog(target, now.DialogTitle, button);
            actions.Add("supervisor", "answer", now.DialogTitle + " -> " + button + (done ? "" : " (no button)"));
        }

        async Task Escalate(int step, StateResult result, Snapshot now, Memory memory, Target target, CancellationToken cancel)
        {
            switch (step)
            {
                case 0:
                    actions.Add("supervisor", "escalate-evidence", result + " age " + result.AgeMs + " ms");
                    if (now.ServerUp(memory))
                    {
                        actions.Add("supervisor", "escalate-quit");
                        await control.Quit(target, cancel);
                    }
                    break;
                case 1:
                    actions.Add("supervisor", "escalate-close");
                    control.CloseWindow(target);
                    break;
                case 2:
                    actions.Add("supervisor", "escalate-stop");
                    control.Stop(target);
                    break;
                case 3:
                    if (memory.Recoveries > 0)
                    {
                        actions.Add("supervisor", "fail-run", "a second recovery is not allowed");
                        Failed = true;
                        break;
                    }
                    if (BeforeRestart != null) await BeforeRestart();
                    actions.Add("supervisor", "forget");
                    actions.Add("supervisor", "recovery");
                    try
                    {
                        await control.Start(target, cancel);
                    }
                    catch (StartRefusedException e)
                    {
                        actions.Add("supervisor", "fail-run", "start refused: " + e.Message);
                        Failed = true;
                        break;
                    }
                    actions.Add("supervisor", "own-pid", target.Pid.ToString());
                    stage = -1;
                    break;
            }
        }
    }
}
