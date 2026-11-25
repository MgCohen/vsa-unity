using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class SessionPlugin : Plugin<SessionSettings>
    {
        protected override void Configure(IServiceCollection services, SessionSettings settings)
        {
            services.AddSingleton(settings);
            services.AddSingleton<TestSession>();
            services.AddSingleton<TraceRecorder>();
            services.AddPipeline(new PipelineBuilder<TestRequest>()
                .HandlersOf(typeof(RequestHandler<,>))
                .Runner<MainThreadRunner>()
                .BeforeStep<TraceStartStep>()
                .AfterStep<TraceDoneStep>());
        }
    }
}
