using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    public static class Scripts
    {
        [CliCommand("scripts.run", "Run a test script by name. Each step goes through the pipeline steps")]
        public static Task<ScriptResult> Run([CliArg("name", "The script name, from scripts.list")] string name)
            => SessionBus.Send(new RunScript(name));

        [CliCommand("scripts.list", "List the test scripts")]
        public static Task<ScriptList> List() => SessionBus.Send(new ListScripts());
    }
}
