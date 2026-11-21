using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabObjectsScript : TestScript
    {
        public override string Name => "lab_objects";

        public override async Task Run(ScriptContext script)
        {
            var hidden = SpikeLabFixtures.Add<Image>("Hidden", new Vector2(0, 150), false);
            await script.Send(new AssertState("LabCanvas/LabButton", ObjectState.Active));
            await script.Send(new AssertState("LabCanvas/Hidden", ObjectState.Inactive));
            await script.Send(new AssertState("Missing/Thing", ObjectState.Missing));
            var text = await script.Send(new AssertText("LabCanvas/LabButton/Label", "LabButton"));
            script.Check(text.Text == "LabButton", $"the text is {text.Text}");

            var show = ShowLater(hidden.gameObject, 5);
            var shown = await script.Send(new WaitForObject("LabCanvas/Hidden", ObjectState.Active, 2));
            await show;
            script.Check(shown.Frames >= 5, $"the wait took {shown.Frames} frames, not 5 or more");

            Object.Destroy(hidden.gameObject);
            var gone = await script.Send(new WaitForObject("LabCanvas/Hidden", ObjectState.Missing, 2));
            script.Check(gone.State == ObjectState.Missing, $"the state is {gone.State}");
        }

        static async Task ShowLater(GameObject target, int frames)
        {
            for (var i = 0; i < frames; i++) await Awaitable.NextFrameAsync();
            target.SetActive(true);
        }
    }
}
