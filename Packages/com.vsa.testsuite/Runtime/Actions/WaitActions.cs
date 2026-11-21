using System.Threading.Tasks;

namespace VSA.TestSuite
{
    [ActionName("game.wait_frames")]
    public sealed record WaitFrames(int Count) : IAction<WaitResult>;

    [ActionName("game.wait_seconds")]
    public sealed record WaitSeconds(double Seconds) : IAction<WaitResult>;

    [ActionName("ui.wait_for_object")]
    public sealed record WaitForObject(string Path, ObjectState State = ObjectState.Active, int TimeoutMs = 10000) : IAction<WaitResult>;

    [ActionName("ui.assert_state")]
    public sealed record AssertState(string Path, ObjectState State = ObjectState.Active) : IAction<ObjectState>;

    [ActionName("ui.assert_text")]
    public sealed record AssertText(string Path, string Text) : IAction<string>;

    public sealed class WaitFramesHandler : ActionHandler<WaitFrames, WaitResult>
    {
        readonly WaitSystem wait;
        public WaitFramesHandler(WaitSystem wait) => this.wait = wait;
        protected override Task<WaitResult> Handle(WaitFrames a, ActionContext ctx) => wait.Frames(a.Count, ctx.Cancel);
    }

    public sealed class WaitSecondsHandler : ActionHandler<WaitSeconds, WaitResult>
    {
        readonly WaitSystem wait;
        public WaitSecondsHandler(WaitSystem wait) => this.wait = wait;
        protected override Task<WaitResult> Handle(WaitSeconds a, ActionContext ctx) => wait.Seconds(a.Seconds, ctx.Cancel);
    }

    public sealed class WaitForObjectHandler : ActionHandler<WaitForObject, WaitResult>
    {
        readonly ObjectSystem objects;
        public WaitForObjectHandler(ObjectSystem objects) => this.objects = objects;
        protected override Task<WaitResult> Handle(WaitForObject a, ActionContext ctx)
            => objects.WaitFor(a.Path, a.State, a.TimeoutMs, ctx.Cancel);
    }

    public sealed class AssertStateHandler : ActionHandler<AssertState, ObjectState>
    {
        readonly ObjectSystem objects;
        public AssertStateHandler(ObjectSystem objects) => this.objects = objects;
        protected override Task<ObjectState> Handle(AssertState a, ActionContext ctx)
            => Task.FromResult(objects.AssertState(a.Path, a.State));
    }

    public sealed class AssertTextHandler : ActionHandler<AssertText, string>
    {
        readonly ObjectSystem objects;
        public AssertTextHandler(ObjectSystem objects) => this.objects = objects;
        protected override Task<string> Handle(AssertText a, ActionContext ctx)
            => Task.FromResult(objects.AssertText(a.Path, a.Text));
    }
}
