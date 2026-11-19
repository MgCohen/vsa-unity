using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public static class Scenes
    {
        [CliCommand("scenes.load", "Load a scene by name or path, and wait until its objects ran Start")]
        public static Task<TraceReply> Load(
            [CliArg("name", "Scene name or path, for example SpikeLabScene2")] string name,
            [CliArg("additive", "True keeps the open scenes; false replaces them")] bool additive = false)
            => SessionBus.Send(new LoadScene(name, additive));
    }
}
