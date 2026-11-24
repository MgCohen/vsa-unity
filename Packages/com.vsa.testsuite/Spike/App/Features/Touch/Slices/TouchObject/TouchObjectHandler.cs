using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Spike.Features.Objects;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class TouchObjectHandler : RequestHandler<TouchObject, TouchObjectResult>
    {
        readonly ICommandSender commands;

        public TouchObjectHandler(ICommandSender commands) => this.commands = commands;

        public override async Task<Result<TouchObjectResult>> HandleAsync(IEnvelope<TouchObject> envelope)
        {
            var found = await commands.SendAsync(new LocateObject(envelope.Message.Path), envelope.Cancellation);
            if (!found.IsSuccess) return found.Error;
            var touched = await commands.SendAsync(new TouchPointer(found.Value.Point), envelope.Cancellation);
            if (!touched.IsSuccess) return touched.Error;
            var value = found.Value;
            return new TouchObjectResult(value.Path, value.Name, value.Point, touched.Value.Hit);
        }
    }
}
