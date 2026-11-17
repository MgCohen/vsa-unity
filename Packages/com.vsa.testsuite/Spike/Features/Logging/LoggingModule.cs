using Microsoft.Extensions.DependencyInjection;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The logging feature. Its hooks are VSA Before and After hooks: VSA finds them in the module
    /// and runs them for each message, also for requests. The module adds the services of the hooks.
    /// </summary>
    public sealed class LoggingModule : VSA.Module.Module
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddSingleton<StepLog>();
    }
}
