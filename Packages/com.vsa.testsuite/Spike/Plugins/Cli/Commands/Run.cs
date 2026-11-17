using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Commands for the run itself. The CLI calls them by name, and scripts call them
    /// as methods, for example Run.End().
    /// </summary>
    public static class Run
    {
        [CliCommand("run.end", "End the run: stop the VSA app")]
        public static Task<RunEnded> End() => Task.FromResult(new RunEnded(AppBus.End()));
    }
}
