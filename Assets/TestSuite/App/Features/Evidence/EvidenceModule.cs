using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace VSA.TestSuite.Features.Evidence
{
    public sealed class EvidenceModule : Module
    {
        protected override void Configure(IServiceCollection services) => services.AddSingleton<ScreenshotSystem>();
    }
}
