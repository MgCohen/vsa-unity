using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    public static partial class Test
    {
        public static partial class UI
        {
            // "ui.find", not "ui.find_object": the old package already has that name.
            [CliCommand("ui.find", "Find an active object by hierarchy path and return its screen point")]
            public static Task<FindResult> FindObject(
                [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
                => Actions.Run(new FindObject(path));
        }
    }
}
