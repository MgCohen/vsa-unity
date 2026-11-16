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
        public SceneSystem Scenes { get; }
        /// <summary>Add your reset steps here, for example <c>root.Reset.Add(new ClearSaveData())</c>.</summary>
        public ResetSystem Reset { get; }
        /// <summary>Scripts are found by type. Add a script instance here if it needs constructor arguments.</summary>
        public ScriptSystem Scripts { get; }
        public AppSystem App { get; }
        /// <summary>Video recording. Change <c>Recording.Options</c> here, for example the resolution.</summary>
        public RecordingSystem Recording { get; }
        /// <summary>The touch overlay pointer hook. Set <c>Touches.Enabled = false</c> to hide it.</summary>
        public TouchOverlayHook Touches { get; }

        /// <param name="pointerInput">The input path for clicks. Null: the default for this project.</param>
        /// <param name="videoRecorder">The video recorder. Null: the Unity Recorder in the Editor, if it is installed.</param>
        public TestRoot(IPointerInput pointerInput = null, IVideoRecorder videoRecorder = null)
        {
            Wait = new WaitSystem();
            Objects = new ObjectSystem(Wait);
            Pointer = new PointerSystem(pointerInput ?? PointerInputs.Default());
            Scenes = new SceneSystem();
            Reset = new ResetSystem(Scenes);
            Scripts = new ScriptSystem();
            App = new AppSystem();
            Recording = new RecordingSystem(videoRecorder ?? VideoRecorders.Default());
            Touches = new TouchOverlayHook();
            Pointer.Hooks.Add(Touches);
        }

        public HandlerSet DefaultHandlers() => new HandlerSet()
            .Add(new FindObjectHandler(Objects))
            .Add(new ClickScreenHandler(Pointer))
            .Add(new ClickObjectHandler())
            .Add(new WaitFramesHandler(Wait))
            .Add(new WaitSecondsHandler(Wait))
            .Add(new WaitForObjectHandler(Objects))
            .Add(new AssertStateHandler(Objects))
            .Add(new AssertTextHandler(Objects))
            .Add(new HoldScreenHandler(Pointer))
            .Add(new HoldObjectHandler())
            .Add(new DragScreenHandler(Pointer))
            .Add(new DragObjectHandler())
            .Add(new LoadSceneHandler(Scenes))
            .Add(new ResetGameHandler(Reset))
            .Add(new RunScriptHandler(Scripts))
            .Add(new ListScriptsHandler(Scripts))
            .Add(new ReadInfoHandler(App))
            .Add(new StartRecordingHandler(Recording))
            .Add(new StopRecordingHandler(Recording))
            .Add(new ReadRecordingHandler(Recording))
            .Add(new ShowTouchesHandler(Touches));

        // Hooks run Before in this order and After in the reverse order,
        // so the console hook sees the log lines of all other hooks. The recording hook is last:
        // its After runs first, so the video stops before the other hooks write their files.
        public List<IActionHook> DefaultHooks() => new List<IActionHook>
        {
            new ConsoleHook(), new LogHook(), new StepRecordHook(), new SummaryHook(), new ScreenshotOnFailHook(),
            new RecordScriptHook(Recording),
        };

        public ActionRuntime Build() => Build(DefaultHandlers(), DefaultHooks());

        public ActionRuntime Build(HandlerSet handlers, IReadOnlyList<IActionHook> hooks)
        {
            var runtime = new ActionRuntime(new ActionPipeline(handlers, hooks), new RunFolder(RunFolder.DefaultRoot()));
            Scripts.Attach(runtime);
            return runtime;
        }
    }
}
