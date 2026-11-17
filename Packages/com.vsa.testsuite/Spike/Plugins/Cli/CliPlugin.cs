namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The CLI plugin: the bridge from the static CLI commands to the app bus, and the run commands.
    /// It has no services to add now.
    /// </summary>
    public sealed class CliPlugin : VSA.Module.Plugin
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
        {
        }
    }
}
