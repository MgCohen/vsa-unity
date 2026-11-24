using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    public static class Evidence
    {
        [CliCommand("evidence.screenshot", "Save the screen to a PNG file in the run folder")]
        public static Task<TraceReply> Screenshot(
            [CliArg("name", "The file name, with no extension. Empty: screenshot_<frame>")] string name = "")
            => SessionBus.Send(new TakeScreenshot(name));

        [CliCommand("evidence.folder", "Give the session id and the run folder")]
        public static Task<TraceReply> Folder() => SessionBus.Send(new GetFolder());
    }
}
