namespace VSA.StatusChecker
{
    /// <summary>Reads the state now with one sample, when no daemon runs. The result has no history:
    /// "cold-start" can also be a domain reload, and no clock has run.</summary>
    public static class StatusReader
    {
        public static async Task<StatusReport> Read(Target target, CancellationToken cancel = default)
        {
            var waiter = new StatusWaiter(target);
            var result = await waiter.Sample(cancel);
            return Report(result, waiter.LastSnapshot, target, history: false);
        }

        public static StatusReport Report(StateResult result, Snapshot snapshot, Target target, bool history) => new()
        {
            State = result?.State,
            Result = result?.Result,
            Action = result?.Action ?? StateAction.None,
            AgeMs = result?.AgeMs ?? 0,
            SinceProgressMs = result?.SinceProgressMs ?? 0,
            History = history,
            Pid = target.Pid,
            EditorId = target.EditorId,
            RunFolder = target.RunFolder,
            Signals = snapshot,
        };
    }
}
