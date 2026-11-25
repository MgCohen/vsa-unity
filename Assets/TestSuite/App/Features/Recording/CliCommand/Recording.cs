using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Recording
{
    public static class Recording
    {
        [CliCommand("recording.start", "Start a video in the run folder. A running video stops first")]
        public static Task<TraceReply> Start(
            [CliArg("name", "The file name, with no extension. Empty: video")] string name = "")
            => SessionBus.Send(new StartRecording(name));

        [CliCommand("recording.stop", "Stop the video. With no video, it gives the last one")]
        public static Task<TraceReply> Stop() => SessionBus.Send(new StopRecording());

        [CliCommand("recording.status", "Give the recorder, and the running or the last video")]
        public static Task<TraceReply> Status() => SessionBus.Send(new ReadRecording());
    }
}
