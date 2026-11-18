using VSA.TestSuite.Client.Status;
using Xunit;

namespace VSA.TestSuite.Client.Tests
{
    public class SupervisorTests
    {
        const int OwnPid = 100;

        readonly ReplayClock clock = new();
        readonly FakeControl control = new();
        readonly Target target = new() { Pid = OwnPid };
        readonly Memory memory = new() { OwnPid = OwnPid };
        readonly ActionLog actions;
        readonly Supervisor supervisor;

        public SupervisorTests()
        {
            actions = new ActionLog(clock);
            supervisor = new Supervisor(control, actions, clock) { Mode = SupervisorMode.Live };
        }

        static StateResult Result(string state, StateAction action, string result = null)
            => new() { State = state, Action = result is "stuck" or "stalled" ? StateAction.Escalate : action, Result = result };

        /// <summary>A sample of an alive Editor. serverUp: the own server answers.</summary>
        static Snapshot Alive(bool serverUp, bool probeOk = false)
            => new() { ProcAlive = true, StatusHttp = serverUp ? 200 : 0, DescPresent = serverUp, DescPid = serverUp ? OwnPid : 0, ProbeOk = probeOk };

        Task Step(long atMs, StateResult result, Snapshot now)
        {
            clock.NowMs = atMs;
            return supervisor.Step(result, now, memory, target, CancellationToken.None);
        }

        IEnumerable<string> Kinds => actions.Entries.Select(e => e.Kind);

        [Fact]
        public async Task Off_does_nothing()
        {
            supervisor.Mode = SupervisorMode.Off;
            await Step(0, Result("frozen", StateAction.Wait, "stalled"), Alive(true));
            await Step(0, Result("native-dialog", StateAction.Answer), Alive(true));
            Assert.Empty(control.Calls);
            Assert.Empty(actions.Entries);
        }

        [Theory]
        [InlineData("native-dialog", "OK")]
        [InlineData("safe-mode-prompt", "Ignore")]
        [InlineData("backup-prompt", "Delete")]
        public async Task Answers_a_dialog_with_the_button_of_its_policy(string state, string button)
        {
            var now = Alive(true);
            now.DialogTitle = "Some title";
            await Step(0, Result(state, StateAction.Answer), now);
            Assert.Equal(new[] { "answer Some title -> " + button }, control.Calls);
            Assert.Contains("answer", Kinds);
        }

        [Fact]
        public async Task Safe_mode_closes_the_window_and_fails_without_a_restart()
        {
            await Step(0, Result("safe-mode", StateAction.Fail), Alive(false));
            Assert.Equal(new[] { "close" }, control.Calls);
            Assert.True(supervisor.Failed);
            await Step(60_000, Result("safe-mode", StateAction.Fail), Alive(false));
            Assert.Equal(new[] { "close" }, control.Calls);
        }

        [Fact]
        public async Task Escalates_quit_close_stop_then_restarts_with_15_s_between_steps()
        {
            var stuck = Result("domain-reload", StateAction.Wait, "stuck");
            await Step(0, stuck, Alive(true));
            Assert.Equal(new[] { "quit" }, control.Calls);

            await Step(14_000, stuck, Alive(true));
            Assert.Equal(new[] { "quit" }, control.Calls);

            await Step(15_000, stuck, Alive(true));
            Assert.Equal(new[] { "quit", "close" }, control.Calls);

            await Step(30_000, stuck, Alive(true));
            Assert.Equal(new[] { "quit", "close", "stop" }, control.Calls);

            await Step(31_000, stuck, new Snapshot { ProcAlive = false });
            Assert.Equal(new[] { "quit", "close", "stop", "start" }, control.Calls);
            Assert.Equal(new[] { "escalate-evidence", "escalate-quit", "escalate-close", "escalate-stop", "forget", "recovery", "own-pid" }, Kinds);
            Assert.Equal("4242", actions.Entries.Last().Detail);
            Assert.False(supervisor.Failed);
        }

        [Fact]
        public async Task Does_not_quit_when_the_server_is_down()
        {
            await Step(0, Result("domain-reload", StateAction.Wait, "stuck"), Alive(false));
            Assert.Empty(control.Calls);
            Assert.Equal(new[] { "escalate-evidence" }, Kinds);
        }

        [Fact]
        public async Task Never_does_a_second_recovery()
        {
            memory.Recoveries = 1;
            await Step(0, Result("frozen", StateAction.Wait, "stalled"), Alive(false));
            await Step(1_000, Result("frozen", StateAction.Wait, "stalled"), new Snapshot { ProcAlive = false });
            Assert.DoesNotContain("start", control.Calls);
            Assert.True(supervisor.Failed);
            Assert.Contains("fail-run", Kinds);
        }

        [Fact]
        public async Task Fails_when_the_start_is_refused()
        {
            control.StartFails = new StartRefusedException("compile check failed", new[] { "error CS1002" }, "check.log");
            await Step(0, Result("frozen", StateAction.Wait, "stalled"), Alive(false));
            await Step(1_000, Result("frozen", StateAction.Wait, "stalled"), new Snapshot { ProcAlive = false });
            Assert.Contains("start", control.Calls);
            Assert.True(supervisor.Failed);
            Assert.Contains(actions.Entries, e => e.Kind == "fail-run" && e.Detail.Contains("start refused"));
        }

        [Fact]
        public async Task Dry_run_writes_once_and_does_nothing()
        {
            supervisor.Mode = SupervisorMode.DryRun;
            var stalled = Result("frozen", StateAction.Wait, "stalled");
            await Step(0, stalled, Alive(true));
            await Step(20_000, stalled, Alive(true));
            await Step(40_000, stalled, Alive(true));
            Assert.Empty(control.Calls);
            Assert.Equal(new[] { "escalate-dry" }, Kinds);
        }

        [Fact]
        public async Task A_good_probe_ends_the_escalation()
        {
            await Step(0, Result("frozen", StateAction.Wait, "stalled"), Alive(true));
            await Step(5_000, Result("ready-idle", StateAction.None), Alive(true, probeOk: true));
            await Step(30_000, Result("frozen", StateAction.Wait, "stalled"), Alive(true));
            Assert.Equal(new[] { "quit", "quit" }, control.Calls);
        }

        [Fact]
        public async Task Restarts_after_a_crash_only_when_asked()
        {
            var crash = Result("crash", StateAction.Start);
            var gone = new Snapshot { ProcAlive = false, ProcExitCode = unchecked((int)0xC0000005) };
            await Step(0, crash, gone);
            Assert.Empty(control.Calls);

            supervisor.RestartOnCrash = true;
            await Step(1_000, crash, Alive(false));
            Assert.Empty(control.Calls);
            await Step(2_000, crash, gone);
            Assert.Equal(new[] { "start" }, control.Calls);
        }
    }
}
