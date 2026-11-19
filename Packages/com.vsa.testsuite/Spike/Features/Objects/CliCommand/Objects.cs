using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public static class Objects
    {
        [CliCommand("objects.find", "Find an active object by hierarchy path and return its screen point")]
        public static Task<FindResult> Find(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => SessionBus.Send(new FindObject(path));

        [CliCommand("objects.wait_for", "Wait until an active object is at the path; fail after the time limit")]
        public static Task<WaitResult> WaitFor(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path,
            [CliArg("seconds", "Time limit in seconds of real time")] float seconds = 5)
            => SessionBus.Send(new WaitForObject(path, seconds));
    }
}
