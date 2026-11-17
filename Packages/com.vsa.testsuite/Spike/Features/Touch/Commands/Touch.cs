using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Commands of the touch feature. The CLI calls them by name, and scripts call them
    /// as methods, for example Touch.Screen(489, 249).
    /// </summary>
    public static class Touch
    {
        [CliCommand("touch.screen", "Touch a screen point, in pixels from the lower left")]
        public static Task<TouchResult> Screen(
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y)
            => AppBus.Send(new TouchScreen(new Point(x, y)));

        [CliCommand("touch.object", "Find an active object by hierarchy path and touch its screen point")]
        public static Task<TouchObjectResult> Object(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => AppBus.Send(new TouchObject(path));
    }
}
