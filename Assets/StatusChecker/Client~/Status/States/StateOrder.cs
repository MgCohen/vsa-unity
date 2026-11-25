namespace VSA.StatusChecker
{
    /// <summary>All states in check order. The first match wins. Add a new state before the fallback of its group:
    /// NativeDialog (dialogs), ColdStart (no server), ReadyIdle (probe answers), Frozen (all).</summary>
    public static class States
    {
        public static readonly IReadOnlyList<IEditorState> All = new IEditorState[]
        {
            new ForeignEditor(),
            new StaleDescriptor(),
            new NoEditor(),
            new Exited(),
            new StartFailed(),
            new Crash(),
            new SafeModePrompt(),
            new BackupPrompt(),
            new NativeDialog(),
            new NotAutomated(),
            new SafeMode(),
            new DescriptorMismatch(),
            new PlayTransition(),
            new ServerDown(),
            new DomainReload(),
            new ColdStart(),
            new Settling(),
            new Compiling(),
            new CompileFailed(),
            new Importing(),
            new PlayPaused(),
            new PlayRunning(),
            new ReadyIdle(),
            new QueueFull(),
            new LongCommand(),
            new Frozen(),
        };
    }
}
