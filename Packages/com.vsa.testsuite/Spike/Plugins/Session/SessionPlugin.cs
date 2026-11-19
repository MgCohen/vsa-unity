using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class SessionPlugin : Plugin
    {
        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<TestSession>();
            services.AddSingleton<StepLog>();
            services.AddPipeline(new PipelineBuilder<TestRequest>()
                .HandlersOf(typeof(RequestHandler<,>))
                .Runner<MainThreadRunner>()
                .BeforeStep<LogStartStep>()
                .AfterStep<LogDoneStep>());
        }
    }
}
