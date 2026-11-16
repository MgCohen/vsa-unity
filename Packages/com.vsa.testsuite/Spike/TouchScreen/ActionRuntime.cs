using System.Threading;
using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>Runs an action with its handler. Hooks come later.</summary>
    internal sealed class ActionRuntime
    {
        readonly ActionRegistry registry;
        readonly CancellationTokenSource stop = new CancellationTokenSource();

        public ActionRuntime(ActionRegistry registry) => this.registry = registry;

        public async Task<TResult> Run<TResult>(IAction<TResult> action)
        {
            var handler = registry.Find(action.GetType());
            return (TResult)await handler.Run(action, new ActionContext(stop.Token));
        }
    }
}
