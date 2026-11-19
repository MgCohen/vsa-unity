using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class TouchScreenHandler : RequestHandler<TouchScreen, TouchResult>
    {
        readonly TouchSystem touch;

        public TouchScreenHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<TouchResult>> HandleAsync(Envelope<TouchScreen> envelope)
        {
            var hit = await touch.Touch(envelope.Message.Point, envelope.Cancellation);
            return new TouchResult(envelope.Message.Point, hit != null ? hit.name : null);
        }
    }
}
