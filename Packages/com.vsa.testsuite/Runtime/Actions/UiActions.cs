using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite
{
    [ActionName("ui.find_object")]
    public sealed record FindObject(string Path) : IAction<ObjectRef>;

    [ActionName("ui.click_screen")]
    public sealed record ClickScreen(float X, float Y, [property: Newtonsoft.Json.JsonIgnore] GameObject Expected = null) : IAction<ClickResult>;

    [ActionName("ui.click_object")]
    public sealed record ClickObject(string Path) : IAction<ClickResult>;

    /// <summary>Routes to the object system.</summary>
    public sealed class FindObjectHandler : ActionHandler<FindObject, ObjectRef>
    {
        readonly ObjectSystem objects;
        public FindObjectHandler(ObjectSystem objects) => this.objects = objects;
        protected override Task<ObjectRef> Handle(FindObject a, ActionContext ctx) => Task.FromResult(objects.Find(a.Path));
    }

    /// <summary>Routes to the pointer system.</summary>
    public sealed class ClickScreenHandler : ActionHandler<ClickScreen, ClickResult>
    {
        readonly PointerSystem pointer;
        public ClickScreenHandler(PointerSystem pointer) => this.pointer = pointer;
        protected override Task<ClickResult> Handle(ClickScreen a, ActionContext ctx)
            => pointer.Click(new Vector2(a.X, a.Y), a.Expected, ctx.Cancel);
    }

    /// <summary>Composite: find the object, then click its screen point. No logic.</summary>
    public sealed class ClickObjectHandler : ActionHandler<ClickObject, ClickResult>
    {
        protected override async Task<ClickResult> Handle(ClickObject a, ActionContext ctx)
        {
            var obj = await ctx.Run(new FindObject(a.Path));
            return await ctx.Run(new ClickScreen(obj.ScreenPoint[0], obj.ScreenPoint[1], obj.GameObject));
        }
    }
}
