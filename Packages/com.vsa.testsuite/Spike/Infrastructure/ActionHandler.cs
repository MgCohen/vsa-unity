using System;
using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    internal abstract class ActionHandler<TAction, TResult> : IActionHandler where TAction : IAction<TResult>
    {
        public Type ActionType => typeof(TAction);

        public async Task<object> Run(object action, ActionContext context)
            => await Handle((TAction)action, context);

        protected abstract Task<TResult> Handle(TAction action, ActionContext context);
    }
}
