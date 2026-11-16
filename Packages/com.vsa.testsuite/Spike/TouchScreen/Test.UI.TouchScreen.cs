using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>The entry API. The CLI and scripts call the same methods.</summary>
    public static partial class Test
    {
        public static partial class UI
        {
            [CliCommand("ui.touch_screen", "Touch a screen point, in pixels from the lower left")]
            public static Task<TouchResult> TouchScreen(
                [CliArg("x", "Pixels from the left")] float x,
                [CliArg("y", "Pixels from the bottom")] float y)
                => Actions.Run(new TouchScreen(new Vector2(x, y)));
        }
    }
}
