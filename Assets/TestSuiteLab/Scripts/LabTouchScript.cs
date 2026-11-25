using System.Threading.Tasks;
using UnityEngine;
using VSA.TestSuite.Features.Touch;
using VSA.TestSuite.Features.Wait;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabTouchScript : TestScript
    {
        public override string Name => "lab_touch";

        public override async Task Run(ScriptContext script)
        {
            var pad = LabFixtures.Add<LabHoldPad>("HoldPad", new Vector2(-250, 0));
            var item = LabFixtures.Add<LabDragItem>("DragItem", new Vector2(-150, -170));
            var zone = LabFixtures.Add<LabDropZone>("DropZone", new Vector2(150, -170));
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
            var before = LabCounter.Clicks;
            await script.Send(new TouchObject("LabCanvas/LabButton"));
            await script.Send(new ShowTouches(true));
            script.Check(!off.On && LabCounter.Clicks == before + 1,
                $"overlay {off.On}, clicks {LabCounter.Clicks}");

            Object.Destroy(pad.gameObject);
            Object.Destroy(item.gameObject);
            Object.Destroy(zone.gameObject);
        }
    }
}
