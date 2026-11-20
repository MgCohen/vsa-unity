using Umpire.Unity;
using VSA.TestSuite.Spike.Packs.TestSuite;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.App.Host
{
    public sealed class SpikeShell : Shell<TestSuite<SpikeHost>>
    {
        protected override void OnStop() => SessionBus.AppStopping();
    }
}
