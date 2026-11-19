using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public static class Touch
    {
        [CliCommand("touch.screen", "Touch a screen point, in pixels from the lower left")]
        public static Task<TouchResult> Screen(
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y)
            => SessionBus.Send(new TouchScreen(new Point(x, y)));

        [CliCommand("touch.object", "Find an active object by hierarchy path and touch its screen point")]
        public static Task<TouchObjectResult> Object(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => SessionBus.Send(new TouchObject(path));
    }
}
