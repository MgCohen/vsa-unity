using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Recording
{
    internal sealed class ReadRecordingHandler : RequestHandler<ReadRecording, RecordingStatus>
    {
        readonly RecordingSystem recording;

        public ReadRecordingHandler(RecordingSystem recording) => this.recording = recording;

        public override Task<Result<RecordingStatus>> HandleAsync(Envelope<ReadRecording> envelope)
        {
            var file = (recording.Current ?? recording.Last)?.File;
            var active = recording.Current != null;
            var status = new RecordingStatus(recording.RecorderName, recording.Unavailable, active, file);
            return Task.FromResult<Result<RecordingStatus>>(status);
        }
    }
}
