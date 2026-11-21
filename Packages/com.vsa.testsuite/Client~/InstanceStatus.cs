using VSA.TestSuite.Client.Status;

namespace VSA.TestSuite.Client
{
    /// <summary>
    /// The state of an instance that the client attached to, from the status checker of Status/.
    /// It keeps one status tracker, so the clocks and the memory grow over the calls.
    /// It reads only: it never starts, stops or answers anything.
    /// </summary>
    public sealed class InstanceStatus
    {
        readonly StatusWaiter waiter;

        public InstanceStatus(Instance instance)
        {
            var target = new Target
            {
                DescriptorFile = instance.DescriptorPath,
                Probe = instance.IsPlayer ? Target.PlayerProbe : Target.EditorProbe,
            };
            if (!instance.IsPlayer)
                target.ProjectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(instance.DescriptorPath), "..", ".."));
            Instance = instance;
            waiter = new StatusWaiter(target) { AdoptDescriptorPid = true };
        }

        public Instance Instance { get; }

        /// <summary>The last result. Null before the first call.</summary>
        public StateResult Last => waiter.Last;

        /// <summary>Takes one sample and returns the state.</summary>
        public Task<StateResult> Check(CancellationToken cancel = default) => waiter.Sample(cancel);

        /// <summary>Waits until the instance is ready (ready-idle, play-running or play-paused).
        /// Throws InstanceException at once for a dialog, a stall, a crash or a failed state, and at the time limit.</summary>
        public Task<StateResult> WaitReady(int timeoutMs, CancellationToken cancel = default)
            => WaitFor(WaitRules.Ready, timeoutMs, "The instance is not ready", cancel);

        /// <summary>Waits until the state is one of <paramref name="states"/>. Throws InstanceException if the wait stops.</summary>
        public async Task<StateResult> WaitFor(IEnumerable<string> states, int timeoutMs, string failure, CancellationToken cancel = default)
        {
            var outcome = await waiter.Wait(new HashSet<string>(states), timeoutMs, cancel);
            if (outcome.Met) return outcome.Result;
            throw new InstanceException(Instance, failure + ": " + outcome.Code + " (" + outcome.Message + ")");
        }
    }
}
