namespace VSA.TestSuite.Spike
{
    /// <summary>Manual composition: installs each module and adds the hooks.</summary>
    internal static class Composition
    {
        public static ActionRuntime Build()
        {
            var registry = new ActionRegistry();
            TouchModule.Install(registry);
            ObjectsModule.Install(registry);
            return new ActionRuntime(registry, new IActionHook[] { new LogHook() });
        }
    }
}
