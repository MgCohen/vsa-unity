using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed class TouchModule : Module
    {
        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<TouchOverlay>();
            services.AddSingleton<TouchSystem>();
        }
    }
}
