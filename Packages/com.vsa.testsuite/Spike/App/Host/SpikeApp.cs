using VSA.Composition;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The VSA app of the spike: the Unity host, the CLI plugin and the two feature modules.
    /// [CliApp] lets the CLI plugin find it and start it on the first command.
    /// </summary>
    [CliApp]
    public sealed class SpikeApp : VsaApp<SpikeApp>
    {
        protected override VsaBuilder Builder(string[] args)
            => base.Builder(args).UseUnityHost().Modules(new CliPlugin(), new TouchModule(), new ObjectsModule());
    }
}
