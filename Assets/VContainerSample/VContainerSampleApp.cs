using Greeter;
using VSA.Composition;
using VSA.Unity;
using VSA.Unity.VContainer;

namespace VContainerSample {
    public sealed class VContainerSampleApp : VsaApp<VContainerSampleApp> {
        protected override VsaBuilder Builder(string[] args) =>
            VsaBuilder.Create(args).Modules(new GreeterModule()).UseUnityHost().UseVContainer();
    }
}
