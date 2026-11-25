using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Features.TestSuite
{
    public sealed class TestSuiteModule : Module
    {
        protected override void Configure(IServiceCollection services) => services.AddSingleton<ScriptCatalog>();
    }
}
