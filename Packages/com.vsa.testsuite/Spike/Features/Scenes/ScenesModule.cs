using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public sealed class ScenesModule : Module
    {
        public override void Configure(IServiceCollection services) => services.AddSingleton<SceneSystem>();
    }
}
