using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Commands of the objects feature. The CLI calls them by name, and scripts call them
    /// as methods, for example Objects.Find("LabCanvas/LabButton").
    /// </summary>
    public static class Objects
    {
        [CliCommand("objects.find", "Find an active object by hierarchy path and return its screen point")]
        public static Task<FindResult> Find(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => AppBus.Send(new FindObject(path));
    }
}
