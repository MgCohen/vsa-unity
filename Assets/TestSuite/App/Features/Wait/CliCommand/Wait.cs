using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Wait
{
    public static class Wait
    {
        [CliCommand("wait.frames", "Wait for a number of frames")]
        public static Task<TraceReply> Frames(
            [CliArg("count", "Number of frames")] int count = 1)
            => SessionBus.Send(new WaitFrames(count));

        [CliCommand("wait.seconds", "Wait for a time in seconds of real time")]
        public static Task<TraceReply> Seconds(
            [CliArg("seconds", "Time in seconds of real time")] float seconds = 1)
            => SessionBus.Send(new WaitSeconds(seconds));
    }
}
