using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Recording
{
    internal sealed class StartRecordingHandler : RequestHandler<StartRecording, StartResult>
    {
        readonly RecordingSystem recording;
        readonly TestSession session;

        public StartRecordingHandler(RecordingSystem recording, TestSession session)
        {
            this.recording = recording;
            this.session = session;
        }

        public override async Task<Result<StartResult>> HandleAsync(IEnvelope<StartRecording> envelope)
        {
            await Awaitable.MainThreadAsync();
            var name = string.IsNullOrEmpty(envelope.Message.Name) ? "video" : envelope.Message.Name;
            var video = recording.Start(session.Folder, name);
            return new StartResult(recording.RecorderName, video.File);
        }
    }
}
