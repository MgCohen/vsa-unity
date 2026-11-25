using VSA.StatusChecker;
using Xunit;

namespace VSA.StatusChecker.Tests
{
    public class TrackerTests
    {
        const int OwnPid = 100;

        readonly ReplayClock clock = new();
        readonly StatusTracker tracker;

        public TrackerTests()
        {
            var actions = new ActionLog(clock);
            actions.Add(new ActionEntry { T = 0, Who = "test", Kind = "own-pid", Detail = OwnPid.ToString() });
            tracker = new StatusTracker(clock, actions, Limits.Load());
        }

        StateResult At(long ms, Snapshot s)
        {
            clock.NowMs = ms;
            s.T = ms;
            return tracker.Observe(s);
        }

        /// <summary>The server answers, the probe does not, and the beacon is still: the frozen fallback.</summary>
        static Snapshot Frozen(double cpuMs, long beaconSeq = 7) => new()
        {
            ProcAlive = true, StatusHttp = 200, DescPresent = true, DescPid = OwnPid, DescPidAlive = true,
            ProbeHttp = 504, ProcCpuMs = cpuMs, BeaconOwn = true, BeaconSeq = beaconSeq, BeaconAgeS = 30,
        };

        [Fact]
        public void Cpu_time_is_not_progress()
        {
            for (var t = 0; t <= 59_000; t += 1_000)
                Assert.Null(At(t, Frozen(cpuMs: t * 10)).Result);
            var r = At(61_000, Frozen(cpuMs: 999_999));
            Assert.Equal("frozen", r.State);
            Assert.Equal("stalled", r.Result);
            Assert.Equal(StateAction.Escalate, r.Action);
        }

        [Fact]
        public void A_moving_busy_bar_is_progress()
        {
            for (var t = 0; t <= 120_000; t += 1_000)
            {
                var s = Frozen(cpuMs: 0);
                s.BusyBar = t / 1_000;
                Assert.Null(At(t, s).Result);
            }
        }

        [Fact]
        public void A_cap_of_0_means_no_cap()
        {
            for (var t = 0; t <= 3_600_000; t += 10_000)
            {
                var s = new Snapshot
                {
                    ProcAlive = true, StatusHttp = 200, DescPresent = true, DescPid = OwnPid, DescPidAlive = true,
                    ProbeOk = true, ProbeUpdating = true, LogSize = t,
                };
                var r = At(t, s);
                Assert.Equal("importing", r.State);
                Assert.Null(r.Result);
            }
        }

        [Fact]
        public void A_state_past_its_cap_is_stuck_also_with_progress()
        {
            tracker.Observe(new Snapshot { T = 0, ProcAlive = true, StatusHttp = 200, DescPresent = true, DescPid = OwnPid, ProbeOk = true });
            StateResult r = null;
            for (var t = 1_000; t <= 122_000; t += 1_000)
                r = At(t, new Snapshot { ProcAlive = true, LogSize = t });
            Assert.Equal("domain-reload", r.State);
            Assert.Equal("stuck", r.Result);
        }

        [Fact]
        public void The_same_dialog_three_times_is_a_loop()
        {
            StateResult r = null;
            for (var i = 0; i < 3; i++)
            {
                At(i * 2_000, new Snapshot { ProcAlive = true, DialogTitle = "Hello" });
                r = At(i * 2_000 + 1_000, new Snapshot { ProcAlive = true, StatusHttp = 200, DescPresent = true, DescPid = OwnPid, ProbeOk = true });
            }
            Assert.Equal("loop", At(10_000, new Snapshot { ProcAlive = true, DialogTitle = "Hello" }).Result);
        }

        [Fact]
        public void A_long_command_may_run_for_an_hour()
        {
            Assert.True(Limits.Load().TryGet("long-command", false, out var capMs, out _));
            Assert.True(capMs > 3_600_000, "A long command, for example a test script, may run for one hour.");
        }
    }
}
