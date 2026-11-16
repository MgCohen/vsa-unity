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
        [CliCommand("run.end", "End the run: stop the actions that still run, call the end hooks and clear the runtime")]
        public static Task<RunSummary> End() => Task.FromResult(Actions.End());
    }
}
