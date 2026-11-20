using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class Session
    {
        [CliCommand("session.end", "End the session: close the run folder, stop the running actions and the app")]
        public static Task<EndResult> End() => SessionBus.End("session.end");

        [CliCommand("session.info", "Give the platform, frame, time, scene, process and session. Starts no session")]
        public static Task<SessionInfo> Info() => SessionBus.Info();
    }
}
