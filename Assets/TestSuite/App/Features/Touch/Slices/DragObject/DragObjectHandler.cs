using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Features.Objects;

namespace VSA.TestSuite.Features.Touch
{
    internal sealed class DragObjectHandler : RequestHandler<DragObject, DragObjectResult>
    {
        readonly ICommandSender commands;

        public DragObjectHandler(ICommandSender commands) => this.commands = commands;

        public override async Task<Result<DragObjectResult>> HandleAsync(IEnvelope<DragObject> envelope)
        {
            var drag = envelope.Message;
            var from = await commands.SendAsync(new LocateObject(drag.Path), envelope.Cancellation);
            if (!from.IsSuccess) return from.Error;
            var to = await commands.SendAsync(new LocateObject(drag.To), envelope.Cancellation);
            if (!to.IsSuccess) return to.Error;
            var pointer = new DragPointer(from.Value.Point, to.Value.Point, drag.Steps);
            var dragged = await commands.SendAsync(pointer, envelope.Cancellation);
            if (!dragged.IsSuccess) return dragged.Error;
            return new DragObjectResult(
                drag.Path, drag.To, from.Value.Point, to.Value.Point, dragged.Value.Hit, dragged.Value.Frames);
        }
    }
}
