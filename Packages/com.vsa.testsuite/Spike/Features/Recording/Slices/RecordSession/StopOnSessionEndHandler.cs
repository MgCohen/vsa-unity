using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Recording
{
    internal sealed class StopOnSessionEndHandler : IntegrationEventHandler<SessionEnded>
    {
        readonly RecordingSystem recording;

        public StopOnSessionEndHandler(RecordingSystem recording) => this.recording = recording;

        public override Task<Result<Unit>> HandleAsync(Envelope<SessionEnded> envelope)
        {
            recording.Stop("session end: " + envelope.Message.Reason);
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}
