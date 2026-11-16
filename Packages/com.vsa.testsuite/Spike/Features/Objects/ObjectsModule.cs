namespace VSA.TestSuite.Spike
{
    /// <summary>Installs the objects feature: its systems and the handlers of its slices.</summary>
    internal static class ObjectsModule
    {
        public static void Install(ActionRegistry registry)
        {
            var objects = new ObjectSystem();
            registry.Add(new FindObjectHandler(objects));
        }
    }
}
