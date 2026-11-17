using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Commands of the scripts feature. The CLI calls them by name, and scripts call them
    /// as methods, for example Scripts.Run("lab_button").
    /// </summary>
    public static class Scripts
    {
        [CliCommand("scripts.run", "Run a test script by name. Each step goes through the hooks")]
        public static Task<ScriptResult> Run([CliArg("name", "The script name, from scripts.list")] string name)
            => SessionBus.Send(new RunScript(name));

        [CliCommand("scripts.list", "List the test scripts")]
        public static Task<ScriptList> List() => SessionBus.Send(new ListScripts());
    }
}
