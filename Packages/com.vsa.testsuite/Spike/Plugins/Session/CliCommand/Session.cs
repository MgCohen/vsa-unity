using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class Session
    {
        [CliCommand("session.end", "End the session: close the run folder, stop the running actions and the app")]
        public static Task<EndResult> End() => SessionBus.End("session.end");
    }
}
