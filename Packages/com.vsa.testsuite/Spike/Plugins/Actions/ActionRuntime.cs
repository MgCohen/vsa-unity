using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Runs an action with its handler, with the hooks before and after it.
    /// One runtime is one run: End stops the actions that still run and calls the run hooks.
    /// </summary>
    internal sealed class ActionRuntime
    {
        readonly ActionRegistry registry;
        readonly IReadOnlyList<IActionHook> hooks;
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        int actions;
        int failed;

        public ActionRuntime(ActionRegistry registry, IReadOnlyList<IActionHook> hooks)
        {
            this.registry = registry;
            this.hooks = hooks;
        }

        public async Task<TResult> Run<TResult>(IAction<TResult> action)
        {
            // Hooks, handlers and systems use Unity API, so they run on the main thread
            // for every caller, also a thread pool thread.
            await Awaitable.MainThreadAsync();
            var handler = registry.Find(action.GetType());
            actions++;
            foreach (var hook in hooks) hook.Before(action);
            try
            {
                var result = (TResult)await handler.Run(action, new ActionContext(this, stop.Token));
                foreach (var hook in hooks) hook.After(action, result, null);
                return result;
            }
            catch (Exception error)
            {
                failed++;
                foreach (var hook in hooks) hook.After(action, null, error);
                throw;
            }
        }

        public RunSummary End()
        {
            stop.Cancel();
            var summary = new RunSummary(actions, failed);
            foreach (var hook in hooks)
                if (hook is IRunHook runHook) runHook.RunEnded(summary);
            stop.Dispose();
            return summary;
        }
    }
}
