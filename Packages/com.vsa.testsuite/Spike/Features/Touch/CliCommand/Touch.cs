using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public static class Touch
    {
        [CliCommand("touch.screen", "Touch a screen point, in pixels from the lower left")]
        public static Task<TraceReply> Screen(
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y)
            => SessionBus.Send(new TouchScreen(new Point(x, y)));

        [CliCommand("touch.object", "Find an active object by hierarchy path and touch its screen point")]
        public static Task<TraceReply> Object(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => SessionBus.Send(new TouchObject(path));

        [CliCommand("touch.hold_screen", "Press a screen point, keep it down for a time, then release")]
        public static Task<TraceReply> HoldScreen(
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y,
            [CliArg("seconds", "Time to keep it down, in seconds of real time")] float seconds = 0.5f)
            => SessionBus.Send(new HoldScreen(new Point(x, y), seconds));

        [CliCommand("touch.hold_object", "Find an active object by hierarchy path, press it for a time, then release")]
        public static Task<TraceReply> HoldObject(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path,
            [CliArg("seconds", "Time to keep it down, in seconds of real time")] float seconds = 0.5f)
            => SessionBus.Send(new HoldObject(path, seconds));

        [CliCommand("touch.drag_screen", "Drag from one screen point to another")]
        public static Task<TraceReply> DragScreen(
            [CliArg("x", "Start, pixels from the left")] float x,
            [CliArg("y", "Start, pixels from the bottom")] float y,
            [CliArg("to-x", "End, pixels from the left")] float toX,
            [CliArg("to-y", "End, pixels from the bottom")] float toY,
            [CliArg("steps", "Number of move frames")] int steps = 10)
            => SessionBus.Send(new DragScreen(new Point(x, y), new Point(toX, toY), steps));

        [CliCommand("touch.drag_object", "Drag one active object to another, by hierarchy paths")]
        public static Task<TraceReply> DragObject(
            [CliArg("path", "Hierarchy path of the object to drag")] string path,
            [CliArg("to", "Hierarchy path of the target object")] string to,
            [CliArg("steps", "Number of move frames")] int steps = 10)
            => SessionBus.Send(new DragObject(path, to, steps));

        [CliCommand("touch.show", "Show or hide the touch overlay: a burst at each press and a trail for each drag")]
        public static Task<TraceReply> Show(
            [CliArg("on", "True shows the touches; false hides them")] bool on = true)
            => SessionBus.Send(new ShowTouches(on));
    }
}
