using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Commands for the session itself. The CLI calls them by name, and scripts call them
    /// as methods, for example Session.End().
    /// </summary>
    public static class Session
    {
        [CliCommand("session.end", "End the session: stop the running actions and the VSA app")]
        public static Task<SessionEnded> End() => Task.FromResult(new SessionEnded(SessionBus.End()));
    }
}
