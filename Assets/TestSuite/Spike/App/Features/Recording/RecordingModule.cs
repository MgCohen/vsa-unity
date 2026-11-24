using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed class RecordingModule : Module<RecordingSettings>
    {
        protected override void Configure(IServiceCollection services, RecordingSettings settings)
        {
            services.AddSingleton(settings);
            services.AddSingleton(_ => VideoRecorders.Create());
            services.AddSingleton<RecordingSystem>();
        }
    }
}
