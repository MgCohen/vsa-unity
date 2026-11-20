using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class ShowTouchesHandler : RequestHandler<ShowTouches, OverlayResult>
    {
        readonly TouchOverlay overlay;

        public ShowTouchesHandler(TouchOverlay overlay) => this.overlay = overlay;

        public override Task<Result<OverlayResult>> HandleAsync(Envelope<ShowTouches> envelope)
        {
            overlay.On = envelope.Message.On;
            return Task.FromResult<Result<OverlayResult>>(new OverlayResult(overlay.On));
        }
    }
}
