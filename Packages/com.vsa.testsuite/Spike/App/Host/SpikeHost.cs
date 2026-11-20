using Umpire.Unity;
using VSA.TestSuite.Spike.Features.Evidence;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Features.Recording;
using VSA.TestSuite.Spike.Features.Scenes;
using VSA.TestSuite.Spike.Features.Scripts;
using VSA.TestSuite.Spike.Features.Touch;
using VSA.TestSuite.Spike.Features.Wait;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.App.Host
{
    public sealed class SpikeHost : UnityHost
    {
        public SpikeHost()
            : base(
                new SessionPlugin(),
                new TouchModule(),
                new ObjectsModule(),
                new ScriptsModule(),
                new ScenesModule(),
                new WaitModule(),
                new EvidenceModule(),
                new RecordingModule())
        {
        }
    }
}
