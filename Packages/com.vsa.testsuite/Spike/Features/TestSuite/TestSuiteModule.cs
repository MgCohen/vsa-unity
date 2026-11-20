using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.TestSuite
{
    public sealed class TestSuiteModule : Module
    {
        public override void Configure(IServiceCollection services) => services.AddSingleton<ScriptCatalog>();
    }
}
