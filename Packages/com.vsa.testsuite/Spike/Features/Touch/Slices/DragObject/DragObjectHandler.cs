using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Features.Objects;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class DragObjectHandler : RequestHandler<DragObject, DragObjectResult>
    {
        readonly IRequestSender requests;

        public DragObjectHandler(IRequestSender requests) => this.requests = requests;

        public override async Task<Result<DragObjectResult>> HandleAsync(IEnvelope<DragObject> envelope)
        {
            var drag = envelope.Message;
            var from = await requests.SendAsync(new FindObject(drag.Path), envelope.Cancellation);
            if (!from.IsSuccess) return from.Error;
            var to = await requests.SendAsync(new FindObject(drag.To), envelope.Cancellation);
            if (!to.IsSuccess) return to.Error;
            var screen = new DragScreen(from.Value.Point, to.Value.Point, drag.Steps);
            var dragged = await requests.SendAsync(screen, envelope.Cancellation);
            if (!dragged.IsSuccess) return dragged.Error;
            return new DragObjectResult(
                drag.Path, drag.To, from.Value.Point, to.Value.Point, dragged.Value.Hit, dragged.Value.Frames);
        }
    }
}
