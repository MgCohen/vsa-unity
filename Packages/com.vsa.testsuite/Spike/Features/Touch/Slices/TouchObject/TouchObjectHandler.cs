using System.Threading.Tasks;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.TestSuite.Spike
{
    /// <summary>No logic of its own: it sends FindObject, then TouchScreen at the point found.</summary>
    internal sealed class TouchObjectHandler : RequestHandler<TouchObject, TouchObjectResult>
    {
        readonly IBus bus;

        public TouchObjectHandler(IBus bus) => this.bus = bus;

        public override async Task<Result<TouchObjectResult>> HandleAsync(Envelope<TouchObject> envelope)
        {
            var found = await bus.Send(new FindObject(envelope.Message.Path), envelope.Cancellation);
            if (!found.IsSuccess) return found.Error;
            var touched = await bus.Send(new TouchScreen(found.Value.Point), envelope.Cancellation);
            if (!touched.IsSuccess) return touched.Error;
            return new TouchObjectResult(found.Value.Path, found.Value.Name, found.Value.Point, touched.Value.Hit);
        }
    }
}
