using System.Threading.Tasks;
using UnityEngine;
using VSA.TestSuite.Spike.Features.Scenes;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabSceneEndScript : TestScript
    {
        public override string Name => "lab_scene_end";

        public override async Task Run(ScriptContext script)
        {
            var load = script.Send(new LoadScene("SpikeLabScene2", false));
            await Awaitable.NextFrameAsync();
            Debug.Log($"[Lab] session.end during the load: stopped={SessionBus.End()}");
            await load;
        }
    }
}
