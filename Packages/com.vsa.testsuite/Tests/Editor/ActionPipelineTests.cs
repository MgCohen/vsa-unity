using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VSA.TestSuite.Editor.Tests
{
    public sealed class ActionPipelineTests
    {
        sealed record Add(int A, int B) : IAction<int>;
        sealed record AddThree(int A, int B, int C) : IAction<int>;
        sealed record Fail(string Reason) : IAction<Unit>;
        sealed record Orphan : IAction<Unit>;

        sealed class AddHandler : ActionHandler<Add, int>
        {
            protected override Task<int> Handle(Add a, ActionContext ctx) => Task.FromResult(a.A + a.B);
        }

        // A composite action: routes to two child actions and maps the value.
        sealed class AddThreeHandler : ActionHandler<AddThree, int>
        {
            protected override async Task<int> Handle(AddThree a, ActionContext ctx)
            {
                var ab = await ctx.Run(new Add(a.A, a.B));
                return await ctx.Run(new Add(ab, a.C));
            }
        }

        sealed class FailHandler : ActionHandler<Fail, Unit>
        {
            protected override Task<Unit> Handle(Fail a, ActionContext ctx)
                => throw new ActionFailed(FailCode.AssertFailed, a.Reason);
        }

        sealed class TraceHook : IActionHook
        {
            readonly string id;
            readonly List<string> trace;

            public TraceHook(string id, List<string> trace)
            {
                this.id = id;
                this.trace = trace;
            }

            public Task Before(ActionContext ctx)
            {
                trace.Add(id + ">" + ctx.Name + "@" + ctx.Depth);
                return Task.CompletedTask;
            }

            public Task After(ActionContext ctx)
            {
                trace.Add(id + "<" + ctx.Name + (ctx.Outcome.Failed ? "!" : ""));
                return Task.CompletedTask;
            }
        }

        List<string> trace;
        ActionPipeline pipeline;
        StepFolder step;

        [SetUp]
        public void SetUp()
        {
            trace = new List<string>();
            var handlers = new HandlerSet().Add(new AddHandler()).Add(new AddThreeHandler()).Add(new FailHandler());
            pipeline = new ActionPipeline(handlers, new IActionHook[] { new TraceHook("a", trace), new TraceHook("b", trace) });
            step = new StepFolder("s-test", Path.Combine(Path.GetTempPath(), "vsa-testsuite-tests"));
        }

        Task<T> Run<T>(IAction<T> action) => pipeline.Run(action, null, CancellationToken.None, step);

        [Test]
        public async Task SimpleActionReturnsTheHandlerValue()
        {
            Assert.AreEqual(5, await Run(new Add(2, 3)));
        }

        [Test]
        public async Task HooksRunBeforeInOrderAndAfterInReverse()
        {
            await Run(new Add(1, 1));
            CollectionAssert.AreEqual(new[] { "a>Add@0", "b>Add@0", "b<Add", "a<Add" }, trace);
        }

        [Test]
        public async Task CompositeActionRunsChildrenThroughTheHooks()
        {
            Assert.AreEqual(6, await Run(new AddThree(1, 2, 3)));
            CollectionAssert.AreEqual(new[]
            {
                "a>AddThree@0", "b>AddThree@0",
                "a>Add@1", "b>Add@1", "b<Add", "a<Add",
                "a>Add@1", "b>Add@1", "b<Add", "a<Add",
                "b<AddThree", "a<AddThree",
            }, trace);
        }

        [Test]
        public void FailureReachesTheCallerAndTheAfterHooks()
        {
            var e = Assert.ThrowsAsync<ActionFailed>(() => Run(new Fail("no")));
            Assert.AreEqual(FailCode.AssertFailed, e.Code);
            CollectionAssert.AreEqual(new[] { "a>Fail@0", "b>Fail@0", "b<Fail!", "a<Fail!" }, trace);
        }

        [Test]
        public void ActionWithNoHandlerFailsWithNoHandler()
        {
            var e = Assert.ThrowsAsync<ActionFailed>(() => Run(new Orphan()));
            Assert.AreEqual(FailCode.NoHandler, e.Code);
        }

        [Test]
        public void CancelledTokenStopsTheAction()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.CatchAsync<OperationCanceledException>(() => pipeline.Run(new Add(1, 1), null, cts.Token, step));
        }

        [Test]
        public void ActionNameComesFromTheAttribute()
        {
            Assert.AreEqual("ui.click_object", ActionNames.Of(typeof(ClickObject)));
            Assert.AreEqual("Add", ActionNames.Of(typeof(Add)));
        }
    }
}
