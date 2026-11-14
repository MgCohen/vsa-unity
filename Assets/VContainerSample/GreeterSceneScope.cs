using Greeter;
using VContainer;
using VContainer.Unity;
using VSA.Unity.VContainer;

namespace VContainerSample {
    public sealed class GreeterSceneScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder) =>
            builder.RegisterVsaPresenters(new GreeterModule());
    }
}
