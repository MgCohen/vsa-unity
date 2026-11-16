namespace VSA.TestSuite.Spike
{
    /// <summary>Manual composition: builds the systems, adds the handlers and the hooks.</summary>
    internal static class Composition
    {
        public static ActionRuntime Build()
        {
            var touch = new TouchSystem();
            var registry = new ActionRegistry()
                .Add(new TouchScreenHandler(touch));
            return new ActionRuntime(registry, new IActionHook[] { new LogHook() });
        }
    }
}
