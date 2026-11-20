using System.Threading.Tasks;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Features.Scenes;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabSceneParallelScript : TestScript
    {
        public override string Name => "lab_scene_parallel";

        public override async Task Run(ScriptContext script)
        {
            var load = script.Send(new LoadScene("SpikeLabScene2", false));
            var wait = script.Send(new WaitForObject("Scene2Canvas/Scene2Button", ObjectState.Active, 5));
            await Task.WhenAll(load, wait);
            script.Check(wait.Result.Frames > 0, $"the wait took {wait.Result.Frames} frames");
        }
    }
}
