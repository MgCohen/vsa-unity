using Greeter;
using UnityEngine;
using VContainer;
using VSA.Unity.VContainer;

namespace VContainerSample {
    public sealed class SampleRootScope : VsaLifetimeScope<VContainerSampleApp> {
        [SerializeField] string greeting = "Hello from VContainer";

        protected override void ConfigureScope(IContainerBuilder builder) =>
            builder.RegisterInstance<IGreetingSource>(new InspectorGreeting(greeting));

        protected override void Import(VsaImports imports) => imports.Add<IGreetingSource>();

        sealed class InspectorGreeting : IGreetingSource {
            public InspectorGreeting(string greeting) => Greeting = greeting;

            public string Greeting { get; }
        }
    }
}
