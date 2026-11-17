using VSA.Handlers;
using VSA.Handlers.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The hooks plugin: one pipeline for each IAction request, with the hooks as pipeline steps.
    /// To add a hook, write a BeforeStep or AfterStep in Infrastructure and add it here.
    /// The steps run in the order of the builder. The pipeline does not keep the main thread from one
    /// stage to the next, so each step and handler that uses Unity API goes to the main thread itself.
    /// </summary>
    public sealed class HooksPlugin : VSA.Module.Plugin
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddPipeline(new PipelineBuilder<IAction>()
                .HandlersOf(typeof(RequestHandler<,>))
                .BeforeStep<LogStartStep>()
                .AfterStep<LogDoneStep>());
    }
}
