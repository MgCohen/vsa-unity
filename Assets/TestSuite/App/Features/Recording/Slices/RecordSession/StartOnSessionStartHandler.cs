using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Recording
{
    internal sealed class StartOnSessionStartHandler : IntegrationEventHandler<SessionStarted>
    {
        readonly RecordingSystem recording;
        readonly RecordingSettings settings;

        public StartOnSessionStartHandler(RecordingSystem recording, RecordingSettings settings)
        {
            this.recording = recording;
            this.settings = settings;
        }

        public override async Task<Result<Unit>> HandleAsync(IEnvelope<SessionStarted> envelope)
        {
            if (!settings.RecordSessions) return Unit.Value;
            await Awaitable.MainThreadAsync();
            if (recording.Unavailable != null)
                Debug.LogWarning("[TestSuite] no session video: " + recording.Unavailable);
            else
                recording.Start(envelope.Message.Folder, "session");
            return Unit.Value;
        }
    }
}
