using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace VSA.TestSuite.Features.Touch
{
    internal sealed class TouchPointerHandler : CommandHandler<TouchPointer, PointerHit>
    {
        readonly TouchSystem touch;

        public TouchPointerHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<PointerHit>> HandleAsync(IEnvelope<TouchPointer> envelope)
        {
            var hit = await touch.Touch(envelope.Message.Point, envelope.Cancellation);
            return new PointerHit(hit != null ? hit.name : null);
        }
    }
}
