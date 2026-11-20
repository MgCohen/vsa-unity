using System.Threading.Tasks;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Features.Scenes;
using VSA.TestSuite.Spike.Features.Touch;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabSceneScript : TestScript
    {
        public override string Name => "lab_scene";

        public override async Task Run(ScriptContext script)
        {
            var loaded = await script.Send(new LoadScene("SpikeLabScene2", false));
            script.Check(loaded.Scene == "SpikeLabScene2", $"the load gave {loaded.Scene}");
            await script.Send(new WaitForObject("Scene2Canvas/Scene2Button", ObjectState.Active, 2));
            var touched = await script.Send(new TouchObject("Scene2Canvas/Scene2Button"));
            script.Check(touched.Hit == "Scene2Button", $"the touch hit {touched.Hit}");
            script.Check(SpikeLabCounter.Clicks == 1, $"clicks are {SpikeLabCounter.Clicks}, not 1");
        }
    }
}
