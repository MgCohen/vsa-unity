using System.Threading;
using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>What a handler gets: the cancel token, and a way to run other actions.</summary>
    internal sealed class ActionContext
    {
        readonly ActionRuntime runtime;

        public CancellationToken Cancel { get; }

        public ActionContext(ActionRuntime runtime, CancellationToken cancel)
        {
            this.runtime = runtime;
            Cancel = cancel;
        }

        /// <summary>Runs a sub-action through the runtime, so its hooks run too.</summary>
        public Task<TResult> Run<TResult>(IAction<TResult> action) => runtime.Run(action);
    }
}
