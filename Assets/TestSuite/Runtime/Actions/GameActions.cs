using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite
{
    [ActionName("ui.hold_screen")]
    public sealed record HoldScreen(float X, float Y, int Ms, [property: Newtonsoft.Json.JsonIgnore] GameObject Expected = null) : IAction<ClickResult>;

    [ActionName("ui.hold_object")]
    public sealed record HoldObject(string Path, int Ms) : IAction<ClickResult>;

    [ActionName("ui.drag_screen")]
    public sealed record DragScreen(float FromX, float FromY, float ToX, float ToY, int Steps = 10,
        [property: Newtonsoft.Json.JsonIgnore] GameObject Expected = null) : IAction<DragResult>;

    [ActionName("ui.drag_object")]
    public sealed record DragObject(string Path, string ToPath, int Steps = 10) : IAction<DragResult>;

    [ActionName("game.load_scene")]
    public sealed record LoadScene(string Scene) : IAction<SceneResult>;

    [ActionName("game.reset")]
    public sealed record ResetGame(string Scene = null, int? Seed = null) : IAction<SceneResult>;

    public sealed class HoldScreenHandler : ActionHandler<HoldScreen, ClickResult>
    {
        readonly PointerSystem pointer;
        public HoldScreenHandler(PointerSystem pointer) => this.pointer = pointer;
        protected override Task<ClickResult> Handle(HoldScreen a, ActionContext ctx)
            => pointer.Hold(new Vector2(a.X, a.Y), a.Ms, a.Expected, ctx.Cancel);
    }

    /// <summary>Composite: find the object, then hold its screen point.</summary>
    public sealed class HoldObjectHandler : ActionHandler<HoldObject, ClickResult>
    {
        protected override async Task<ClickResult> Handle(HoldObject a, ActionContext ctx)
        {
            var obj = await ctx.Run(new FindObject(a.Path));
            return await ctx.Run(new HoldScreen(obj.ScreenPoint[0], obj.ScreenPoint[1], a.Ms, obj.GameObject));
        }
    }

    public sealed class DragScreenHandler : ActionHandler<DragScreen, DragResult>
    {
        readonly PointerSystem pointer;
        public DragScreenHandler(PointerSystem pointer) => this.pointer = pointer;
        protected override Task<DragResult> Handle(DragScreen a, ActionContext ctx)
            => pointer.Drag(new Vector2(a.FromX, a.FromY), new Vector2(a.ToX, a.ToY), a.Steps, a.Expected, ctx.Cancel);
    }

    /// <summary>Composite: find both objects, then drag from the first to the second.</summary>
    public sealed class DragObjectHandler : ActionHandler<DragObject, DragResult>
    {
        protected override async Task<DragResult> Handle(DragObject a, ActionContext ctx)
        {
            var from = await ctx.Run(new FindObject(a.Path));
            var to = await ctx.Run(new FindObject(a.ToPath));
            return await ctx.Run(new DragScreen(from.ScreenPoint[0], from.ScreenPoint[1], to.ScreenPoint[0], to.ScreenPoint[1], a.Steps, from.GameObject));
        }
    }

    public sealed class LoadSceneHandler : ActionHandler<LoadScene, SceneResult>
    {
        readonly SceneSystem scenes;
        public LoadSceneHandler(SceneSystem scenes) => this.scenes = scenes;
        protected override Task<SceneResult> Handle(LoadScene a, ActionContext ctx) => scenes.Load(a.Scene, ctx.Cancel);
    }

    public sealed class ResetGameHandler : ActionHandler<ResetGame, SceneResult>
    {
        readonly ResetSystem reset;
        public ResetGameHandler(ResetSystem reset) => this.reset = reset;
        protected override Task<SceneResult> Handle(ResetGame a, ActionContext ctx) => reset.Reset(a.Scene, a.Seed, ctx.Cancel);
    }
}
