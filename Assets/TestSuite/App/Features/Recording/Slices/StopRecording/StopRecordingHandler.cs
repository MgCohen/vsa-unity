using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Features.Recording
{
    internal sealed class StopRecordingHandler : RequestHandler<StopRecording, StopResult>
    {
        readonly RecordingSystem recording;

        public StopRecordingHandler(RecordingSystem recording) => this.recording = recording;

        public override async Task<Result<StopResult>> HandleAsync(IEnvelope<StopRecording> envelope)
        {
            await Awaitable.MainThreadAsync();
            var video = recording.Stop("stopped");
            return video == null
                ? new StopResult(null, 0, null, 0)
                : new StopResult(video.File, video.Seconds, video.StopReason, video.Bytes);
        }
    }
}
