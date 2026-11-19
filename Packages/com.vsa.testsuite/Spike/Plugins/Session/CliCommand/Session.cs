using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class Session
    {
        [CliCommand("session.end", "End the session: stop the running actions and the app")]
        public static Task<SessionEnded> End() => Task.FromResult(new SessionEnded(SessionBus.End()));
    }
}
