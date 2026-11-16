using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>The static entry that commands call. It builds the runtime at the first call.</summary>
    internal static class Actions
    {
        static ActionRuntime runtime;

        public static Task<TResult> Run<TResult>(IAction<TResult> action)
            => (runtime ??= Composition.Build()).Run(action);
    }
}
