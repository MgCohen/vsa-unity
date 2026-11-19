using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    public sealed class ScriptsModule : Module
    {
        public override void Configure(IServiceCollection services) => services.AddSingleton<ScriptCatalog>();
    }
}
