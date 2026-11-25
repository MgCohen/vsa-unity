using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Features.Scenes
{
    public sealed class ScenesModule : Module
    {
        protected override void Configure(IServiceCollection services) => services.AddSingleton<SceneSystem>();
    }
}
