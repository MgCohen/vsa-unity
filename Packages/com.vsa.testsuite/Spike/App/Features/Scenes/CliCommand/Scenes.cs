using System.Globalization;
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

        [CliCommand("scenes.reset", "Set the random seed, then load a scene again in place of the open scenes")]
        public static Task<TraceReply> Reset(
            [CliArg("name", "Scene to load. Empty: the active scene")] string name = "",
            [CliArg("seed", "Seed for UnityEngine.Random. Empty: keep the random state")] string seed = "")
            => SessionBus.Send(new ResetScene(name,
                string.IsNullOrEmpty(seed) ? (int?)null : int.Parse(seed, CultureInfo.InvariantCulture)));
    }
}
