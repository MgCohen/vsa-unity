using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>Runs an action with its handler, with the hooks before and after it.</summary>
    internal sealed class ActionRuntime
    {
        readonly ActionRegistry registry;
        readonly IReadOnlyList<IActionHook> hooks;
        readonly CancellationTokenSource stop = new CancellationTokenSource();

        public ActionRuntime(ActionRegistry registry, IReadOnlyList<IActionHook> hooks)
        {
            this.registry = registry;
            this.hooks = hooks;
        }

        public async Task<TResult> Run<TResult>(IAction<TResult> action)
        {
            var handler = registry.Find(action.GetType());
            foreach (var hook in hooks) hook.Before(action);
            try
            {
                var result = (TResult)await handler.Run(action, new ActionContext(this, stop.Token));
                foreach (var hook in hooks) hook.After(action, result, null);
                return result;
            }
            catch (Exception error)
            {
                foreach (var hook in hooks) hook.After(action, null, error);
                throw;
            }
        }
    }
}
