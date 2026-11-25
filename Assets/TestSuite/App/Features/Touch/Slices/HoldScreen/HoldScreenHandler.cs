using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;

namespace VSA.TestSuite.Features.Touch
{
    internal sealed class HoldScreenHandler : RequestHandler<HoldScreen, HoldResult>
    {
        readonly TouchSystem touch;

        public HoldScreenHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<HoldResult>> HandleAsync(IEnvelope<HoldScreen> envelope)
        {
            var hold = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var hit = await touch.Hold(hold.Point, hold.Seconds, envelope.Cancellation);
            return new HoldResult(hold.Point, hit != null ? hit.name : null,
                Time.frameCount - startFrame, Time.realtimeSinceStartup - startTime);
        }
    }
}
