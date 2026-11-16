using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    public static partial class Test
    {
        public static partial class UI
        {
            [CliCommand("ui.touch_object", "Find an active object by hierarchy path and touch its screen point")]
            public static Task<TouchObjectResult> TouchObject(
                [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
                => Actions.Run(new TouchObject(path));
        }
    }
}
