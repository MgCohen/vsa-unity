namespace VSA.TestSuite.Spike
{
    /// <summary>Manual composition: builds the systems and adds the handlers.</summary>
    internal static class Composition
    {
        public static ActionRuntime Build()
        {
            var touch = new TouchSystem(TouchDevice());
            var registry = new ActionRegistry()
                .Add(new TouchScreenHandler(touch));
            return new ActionRuntime(registry);
        }

        static ITouchDevice TouchDevice()
        {
#if VSA_SPIKE_INPUT_SYSTEM
            return new InputSystemTouchDevice();
#else
            return new UiEventTouchDevice();
#endif
        }
    }
}
