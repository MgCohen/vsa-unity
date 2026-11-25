using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.TestSuite
{
    public static class TestSuite
    {
        [CliCommand("test_suite.run", "Run a test script by name. Each step goes through the pipeline steps")]
        public static Task<TraceReply> Run([CliArg("name", "The script name, from test_suite.list")] string name)
            => SessionBus.Send(new RunScript(name));

        [CliCommand("test_suite.list", "List the test scripts")]
        public static Task<TraceReply> List() => SessionBus.Send(new ListScripts());
    }
}
