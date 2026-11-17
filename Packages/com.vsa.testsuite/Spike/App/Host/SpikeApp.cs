using System;
using UnityEngine;
using VSA.Composition;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>The VSA app of the spike: the Unity host, the CLI plugin and the two feature modules.</summary>
    public sealed class SpikeApp : VsaApp<SpikeApp>
    {
        protected override VsaBuilder Builder(string[] args)
            => base.Builder(args).UseUnityHost().Modules(new CliPlugin(), new TouchModule(), new ObjectsModule());

        // The CLI plugin does not know the app. At Play Mode start, the app gives it its start and stop.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void UseWithCli() => AppBus.Use(() => Run(Array.Empty<string>()), Stop);
    }
}
