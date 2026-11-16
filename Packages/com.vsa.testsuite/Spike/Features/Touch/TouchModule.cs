namespace VSA.TestSuite.Spike
{
    /// <summary>Installs the touch feature: its systems and the handlers of its slices.</summary>
    internal static class TouchModule
    {
        public static void Install(ActionRegistry registry)
        {
            var touch = new TouchSystem();
            registry
                .Add(new TouchScreenHandler(touch))
                .Add(new TouchObjectHandler());
        }
    }
}
