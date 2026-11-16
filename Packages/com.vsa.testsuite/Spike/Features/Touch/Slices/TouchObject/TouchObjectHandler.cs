using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>No logic of its own: it runs FindObject, then TouchScreen at the point found.</summary>
    internal sealed class TouchObjectHandler : ActionHandler<TouchObject, TouchObjectResult>
    {
        protected override async Task<TouchObjectResult> Handle(TouchObject action, ActionContext context)
        {
            var found = await context.Run(new FindObject(action.Path));
            await context.Run(new TouchScreen(new Vector2(found.X, found.Y)));
            return new TouchObjectResult(found.Path, found.Name, found.X, found.Y);
        }
    }
}
