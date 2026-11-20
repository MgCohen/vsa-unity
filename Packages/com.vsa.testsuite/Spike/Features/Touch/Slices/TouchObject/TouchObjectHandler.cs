using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Features.Objects;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class TouchObjectHandler : RequestHandler<TouchObject, TouchObjectResult>
    {
        readonly IRequestSender requests;

        public TouchObjectHandler(IRequestSender requests) => this.requests = requests;

        public override async Task<Result<TouchObjectResult>> HandleAsync(Envelope<TouchObject> envelope)
        {
            var found = await requests.SendAsync(new FindObject(envelope.Message.Path), envelope.Cancellation);
            if (!found.IsSuccess) return found.Error;
            var touched = await requests.SendAsync(new TouchScreen(found.Value.Point), envelope.Cancellation);
            if (!touched.IsSuccess) return touched.Error;
            var value = found.Value;
            return new TouchObjectResult(value.Path, value.Name, value.Point, touched.Value.Hit);
        }
    }
}
