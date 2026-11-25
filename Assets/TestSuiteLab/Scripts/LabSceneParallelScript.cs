using System.Threading.Tasks;
using VSA.TestSuite.Features.Objects;
using VSA.TestSuite.Features.Scenes;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabSceneParallelScript : TestScript
    {
        public override string Name => "lab_scene_parallel";

        public override async Task Run(ScriptContext script)
        {
            var load = script.Send(new LoadScene("TestSuiteLabScene2", false));
            var wait = script.Send(new WaitForObject("Scene2Canvas/Scene2Button", ObjectState.Active, 5));
            await Task.WhenAll(load, wait);
            script.Check(wait.Result.Frames > 0, $"the wait took {wait.Result.Frames} frames");
        }
    }
}
