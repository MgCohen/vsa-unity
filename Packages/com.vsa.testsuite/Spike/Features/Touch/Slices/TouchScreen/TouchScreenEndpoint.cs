using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>Endpoint of the touch screen slice. The CLI and scripts call it.</summary>
    public static class TouchScreenEndpoint
    {
        [CliCommand("ui.touch_screen", "Touch a screen point, in pixels from the lower left")]
        public static Task<TouchResult> Run(
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y)
            => Actions.Run(new TouchScreen(new Point(x, y)));
    }
}
