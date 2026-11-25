using System.Threading.Tasks;
using UnityEngine;
using VSA.TestSuite.Features.Scenes;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabSceneEndScript : TestScript
    {
        public override string Name => "lab_scene_end";

        public override async Task Run(ScriptContext script)
        {
            _ = script.Send(new LoadScene("TestSuiteLabScene2", false));
            await Awaitable.NextFrameAsync();
            Debug.Log("[Lab] the script ends the session during the load");
            script.EndSession();
            Debug.LogError("[Lab] this line must not run");
        }
    }
}
