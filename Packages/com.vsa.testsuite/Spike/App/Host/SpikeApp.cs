using VSA.Composition;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>The VSA app of the spike: the Unity host, the session plugin, and the touch, objects, logging and scripts modules. SpikeHost runs it.</summary>
    public sealed class SpikeApp : VsaApp<SpikeApp>
    {
        protected override VsaBuilder Builder(string[] args)
            => base.Builder(args).UseUnityHost().Modules(new SessionPlugin(), new TouchModule(), new ObjectsModule(), new LoggingModule(), new ScriptsModule());
    }
}
