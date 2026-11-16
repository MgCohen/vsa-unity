using Microsoft.Extensions.DependencyInjection;

namespace VSA.TestSuite.Spike
{
    /// <summary>The objects feature. VSA finds its handlers. The module adds its systems.</summary>
    public sealed class ObjectsModule : VSA.Module.Module
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddSingleton<ObjectSystem>();
    }
}
