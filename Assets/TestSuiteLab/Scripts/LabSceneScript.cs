using System.Threading.Tasks;
using VSA.TestSuite.Features.Objects;
using VSA.TestSuite.Features.Scenes;
using VSA.TestSuite.Features.Touch;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabSceneScript : TestScript
    {
        public override string Name => "lab_scene";

        public override async Task Run(ScriptContext script)
        {
            var loaded = await script.Send(new LoadScene("TestSuiteLabScene2", false));
            script.Check(loaded.Scene == "TestSuiteLabScene2", $"the load gave {loaded.Scene}");
            await script.Send(new WaitForObject("Scene2Canvas/Scene2Button", ObjectState.Active, 2));
            var touched = await script.Send(new TouchObject("Scene2Canvas/Scene2Button"));
            script.Check(touched.Hit == "Scene2Button", $"the touch hit {touched.Hit}");
            script.Check(LabCounter.Clicks == 1, $"clicks are {LabCounter.Clicks}, not 1");
        }
    }
}
