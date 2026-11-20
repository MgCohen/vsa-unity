using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class DragScreenHandler : RequestHandler<DragScreen, DragResult>
    {
        readonly TouchSystem touch;

        public DragScreenHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<DragResult>> HandleAsync(Envelope<DragScreen> envelope)
        {
            var drag = envelope.Message;
            var startFrame = Time.frameCount;
            var hit = await touch.Drag(drag.From, drag.To, drag.Steps, envelope.Cancellation);
            return new DragResult(drag.From, drag.To, Math.Max(1, drag.Steps), hit != null ? hit.name : null,
                Time.frameCount - startFrame);
        }
    }
}
