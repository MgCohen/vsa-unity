using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace VSA.TestSuite.Features.Touch
{
    internal sealed class HoldPointerHandler : CommandHandler<HoldPointer, PointerHold>
    {
        readonly TouchSystem touch;

        public HoldPointerHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<PointerHold>> HandleAsync(IEnvelope<HoldPointer> envelope)
        {
            var hold = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var hit = await touch.Hold(hold.Point, hold.Seconds, envelope.Cancellation);
            return new PointerHold(hit != null ? hit.name : null,
                Time.frameCount - startFrame, Time.realtimeSinceStartup - startTime);
        }
    }
}
