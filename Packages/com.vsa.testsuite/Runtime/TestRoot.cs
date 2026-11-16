using System.Collections.Generic;

namespace VSA.TestSuite
{
    /// <summary>
    /// Manual composition of the runtime. A consumer takes the default handlers and hooks,
    /// adds their own, and sets <see cref="ActionRuntime.Factory"/>.
    /// </summary>
    public sealed class TestRoot
    {
        public WaitSystem Wait { get; }
        public ObjectSystem Objects { get; }
        public PointerSystem Pointer { get; }

        /// <param name="pointerInput">The input path for clicks. Null: the default for this project.</param>
        public TestRoot(IPointerInput pointerInput = null)
        {
            Wait = new WaitSystem();
            Objects = new ObjectSystem(Wait);
            Pointer = new PointerSystem(pointerInput ?? PointerInputs.Default());
        }

        public HandlerSet DefaultHandlers() => new HandlerSet()
            .Add(new FindObjectHandler(Objects))
            .Add(new ClickScreenHandler(Pointer))
            .Add(new ClickObjectHandler())
            .Add(new WaitFramesHandler(Wait))
            .Add(new WaitSecondsHandler(Wait))
            .Add(new WaitForObjectHandler(Objects))
            .Add(new AssertStateHandler(Objects))
            .Add(new AssertTextHandler(Objects));

        // Hooks run Before in this order and After in the reverse order,
        // so the console hook sees the log lines of all other hooks.
        public List<IActionHook> DefaultHooks() => new List<IActionHook>
        {
            new ConsoleHook(), new LogHook(), new StepRecordHook(), new ScreenshotOnFailHook(),
        };

        public ActionRuntime Build() => Build(DefaultHandlers(), DefaultHooks());

        public ActionRuntime Build(HandlerSet handlers, IReadOnlyList<IActionHook> hooks)
            => new ActionRuntime(new ActionPipeline(handlers, hooks), new RunFolder(RunFolder.DefaultRoot()));
    }
}
