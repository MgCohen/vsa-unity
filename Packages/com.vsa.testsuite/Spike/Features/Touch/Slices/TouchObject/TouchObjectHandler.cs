using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>No logic of its own: it runs FindObject, then TouchScreen at the point found.</summary>
    internal sealed class TouchObjectHandler : ActionHandler<TouchObject, TouchObjectResult>
    {
        protected override async Task<TouchObjectResult> Handle(TouchObject action, ActionContext context)
        {
            var found = await context.Run(new FindObject(action.Path));
            var touched = await context.Run(new TouchScreen(found.Point));
            return new TouchObjectResult(found.Path, found.Name, found.Point, touched.Hit);
        }
    }
}
