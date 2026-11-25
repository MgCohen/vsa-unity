using Umpire.Unity;
using VSA.TestSuite.Packs.TestSuite;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.App.Host
{
    public sealed class TestSuiteShell : Shell<TestSuite<TestSuiteHost>>
    {
        protected override void OnStop() => SessionBus.AppStopping();
    }
}
