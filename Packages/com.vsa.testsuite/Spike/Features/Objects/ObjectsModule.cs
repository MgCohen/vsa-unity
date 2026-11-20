using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed class ObjectsModule : Module
    {
        public override void Configure(IServiceCollection services) => services.AddSingleton<ObjectSystem>();
    }
}
