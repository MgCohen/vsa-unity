using System.Threading.Tasks;
using UnityEngine;
using VSA.TestSuite.Spike.Features.Touch;
using VSA.TestSuite.Spike.Features.Wait;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabTouchScript : TestScript
    {
        public override string Name => "lab_touch";

        public override async Task Run(ScriptContext script)
        {
            var pad = SpikeLabFixtures.Add<SpikeLabHoldPad>("HoldPad", new Vector2(-250, 0));
            var item = SpikeLabFixtures.Add<SpikeLabDragItem>("DragItem", new Vector2(-150, -170));
            var zone = SpikeLabFixtures.Add<SpikeLabDropZone>("DropZone", new Vector2(150, -170));
            await script.Send(new WaitFrames(3));

            var held = await script.Send(new HoldObject("LabCanvas/HoldPad", 0.3f));
            script.Check(held.Hit == "HoldPad", $"the hold hit {held.Hit}");
            script.Check(pad.Presses == 1 && pad.HeldSeconds >= 0.3f,
                $"presses {pad.Presses}, held {pad.HeldSeconds} s");

            var dragged = await script.Send(new DragObject("LabCanvas/DragItem", "LabCanvas/DropZone", 5));
            script.Check(item.Begins == 1 && item.Drags == 5 && item.Ends == 1,
                $"begins {item.Begins}, drags {item.Drags}, ends {item.Ends}");
            script.Check(zone.Drops == 1 && zone.Last == "DragItem", $"drops {zone.Drops}, last {zone.Last}");
            script.Check(dragged.Frames >= 7, $"the drag took {dragged.Frames} frames");

            script.Check(GameObject.Find("VSA.TouchOverlay") != null, "no touch overlay after the touches");
            var off = await script.Send(new ShowTouches(false));
            var before = SpikeLabCounter.Clicks;
            await script.Send(new TouchObject("LabCanvas/LabButton"));
            await script.Send(new ShowTouches(true));
            script.Check(!off.On && SpikeLabCounter.Clicks == before + 1,
                $"overlay {off.On}, clicks {SpikeLabCounter.Clicks}");

            Object.Destroy(pad.gameObject);
            Object.Destroy(item.gameObject);
            Object.Destroy(zone.gameObject);
        }
    }
}
