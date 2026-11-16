using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    internal sealed class TouchScreenHandler : ActionHandler<TouchScreen, TouchResult>
    {
        readonly TouchSystem touch;

        public TouchScreenHandler(TouchSystem touch) => this.touch = touch;

        protected override async Task<TouchResult> Handle(TouchScreen action, ActionContext context)
        {
            var hit = await touch.Touch(action.Point, context.Cancel);
            return new TouchResult(action.Point, hit != null ? hit.name : null);
        }
    }
}
