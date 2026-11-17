namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The logging feature. Its hooks are VSA Before and After hooks: VSA finds them in the module
    /// and runs them for each message, also for requests. The module has nothing to add now.
    /// </summary>
    public sealed class LoggingModule : VSA.Module.Module
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
        {
        }
    }
}
