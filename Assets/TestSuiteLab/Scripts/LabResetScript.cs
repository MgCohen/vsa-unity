using System.Threading.Tasks;
using UnityEngine;
using VSA.TestSuite.Features.Scenes;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabResetScript : TestScript
    {
        public override string Name => "lab_reset";

        public override async Task Run(ScriptContext script)
        {
            Random.InitState(42);
            var expected = Random.value;
            new GameObject("LabResetMarker");

            var reset = await script.Send(new ResetScene("", 42));
            var value = Random.value;
            script.Check(reset.Scene == "TestSuiteLab", $"the reset loaded {reset.Scene}");
            script.Check(GameObject.Find("LabResetMarker") == null, "the marker is still in the scene");
            script.Check(value == expected, $"the first random value is {value}, not {expected}");
        }
    }
}
