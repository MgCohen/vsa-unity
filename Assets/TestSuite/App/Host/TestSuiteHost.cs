using Umpire.Unity;
using VSA.TestSuite.Features.Evidence;
using VSA.TestSuite.Features.Objects;
using VSA.TestSuite.Features.Recording;
using VSA.TestSuite.Features.Scenes;
using VSA.TestSuite.Features.TestSuite;
using VSA.TestSuite.Features.Touch;
using VSA.TestSuite.Features.Wait;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.App.Host
{
    public sealed class TestSuiteHost : UnityHost
    {
        public TestSuiteHost()
            : base(
                new SessionPlugin(),
                new TouchModule(),
                new ObjectsModule(),
                new TestSuiteModule(),
                new ScenesModule(),
                new WaitModule(),
                new EvidenceModule(),
                new RecordingModule())
        {
        }
    }
}
