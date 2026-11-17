using Microsoft.Extensions.DependencyInjection;

namespace VSA.TestSuite.Spike
{
    /// <summary>The scripts feature: finds the test scripts and runs them.</summary>
    public sealed class ScriptsModule : VSA.Module.Module
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddSingleton<ScriptCatalog>();
    }
}
