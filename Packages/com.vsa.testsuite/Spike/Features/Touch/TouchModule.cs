using Microsoft.Extensions.DependencyInjection;

namespace VSA.TestSuite.Spike
{
    /// <summary>The touch feature. VSA finds its handlers. The module adds its systems.</summary>
    public sealed class TouchModule : VSA.Module.Module
    {
        public override void Configure(VSA.Module.ModuleBuilder module)
            => module.Services.AddSingleton<TouchSystem>();
    }
}
