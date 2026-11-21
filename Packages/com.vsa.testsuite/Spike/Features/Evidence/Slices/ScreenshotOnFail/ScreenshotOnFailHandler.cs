using System.Threading;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    internal sealed class ScreenshotOnFailHandler : IntegrationEventHandler<StepFailed>
    {
        readonly ScreenshotSystem screenshots;

        public ScreenshotOnFailHandler(ScreenshotSystem screenshots) => this.screenshots = screenshots;

        public override async Task<Result<Unit>> HandleAsync(IEnvelope<StepFailed> envelope)
        {
            await screenshots.Take(envelope.Message.Folder, "screenshot", CancellationToken.None);
            return Unit.Value;
        }
    }
}
