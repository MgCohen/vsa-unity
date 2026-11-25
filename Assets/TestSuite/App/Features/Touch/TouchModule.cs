using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Features.Touch
{
    public sealed class TouchModule : Module
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddSingleton<TouchOverlay>();
            services.AddSingleton<TouchSystem>();
        }
    }
}
