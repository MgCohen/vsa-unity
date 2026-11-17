using VSA.Composition;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>The VSA app of the spike: the Unity host, the CLI and hooks plugins, and the two feature modules. SpikeHost runs it.</summary>
    public sealed class SpikeApp : VsaApp<SpikeApp>
    {
        protected override VsaBuilder Builder(string[] args)
            => base.Builder(args).UseUnityHost().Modules(new CliPlugin(), new HooksPlugin(), new TouchModule(), new ObjectsModule());
    }
}
