using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class DragPointerHandler : CommandHandler<DragPointer, PointerDrag>
    {
        readonly TouchSystem touch;

        public DragPointerHandler(TouchSystem touch) => this.touch = touch;

        public override async Task<Result<PointerDrag>> HandleAsync(IEnvelope<DragPointer> envelope)
        {
            var drag = envelope.Message;
            var startFrame = Time.frameCount;
            var hit = await touch.Drag(drag.From, drag.To, drag.Steps, envelope.Cancellation);
            return new PointerDrag(hit != null ? hit.name : null, Time.frameCount - startFrame);
        }
    }
}
