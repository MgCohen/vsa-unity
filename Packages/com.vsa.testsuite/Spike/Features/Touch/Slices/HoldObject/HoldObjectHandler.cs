using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Features.Objects;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class HoldObjectHandler : RequestHandler<HoldObject, HoldObjectResult>
    {
        readonly IRequestSender requests;

        public HoldObjectHandler(IRequestSender requests) => this.requests = requests;

        public override async Task<Result<HoldObjectResult>> HandleAsync(IEnvelope<HoldObject> envelope)
        {
            var found = await requests.SendAsync(new FindObject(envelope.Message.Path), envelope.Cancellation);
            if (!found.IsSuccess) return found.Error;
            var screen = new HoldScreen(found.Value.Point, envelope.Message.Seconds);
            var held = await requests.SendAsync(screen, envelope.Cancellation);
            if (!held.IsSuccess) return held.Error;
            var value = found.Value;
            return new HoldObjectResult(
                value.Path, value.Name, value.Point, held.Value.Hit, held.Value.Frames, held.Value.Seconds);
        }
    }
}
