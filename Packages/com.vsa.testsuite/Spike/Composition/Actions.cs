using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The static entry that commands call. The first call starts a run. End finishes the run
    /// and clears the static runtime, so the next call starts a new run.
    /// </summary>
    internal static class Actions
    {
        static ActionRuntime runtime;

        public static Task<TResult> Run<TResult>(IAction<TResult> action)
            => (runtime ??= Composition.Build()).Run(action);

        public static RunSummary End()
        {
            var ending = runtime;
            runtime = null;
            return ending?.End() ?? new RunSummary(0, 0);
        }
    }
}
