using Microsoft.Extensions.DependencyInjection;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The session plugin: the base of a test session. It gives the static entry into the app
    /// (SessionBus), the session end, the session service, and the base type of a test script.
    /// </summary>
    public sealed class SessionPlugin : VSA.Module.Plugin
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddSingleton<TestSession>();
    }
}
