using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Spike.Features.Objects;

namespace VSA.TestSuite.Spike.Features.Touch
{
    internal sealed class HoldObjectHandler : RequestHandler<HoldObject, HoldObjectResult>
    {
        readonly ICommandSender commands;

        public HoldObjectHandler(ICommandSender commands) => this.commands = commands;

        public override async Task<Result<HoldObjectResult>> HandleAsync(IEnvelope<HoldObject> envelope)
        {
            var found = await commands.SendAsync(new LocateObject(envelope.Message.Path), envelope.Cancellation);
            if (!found.IsSuccess) return found.Error;
            var pointer = new HoldPointer(found.Value.Point, envelope.Message.Seconds);
            var held = await commands.SendAsync(pointer, envelope.Cancellation);
            if (!held.IsSuccess) return held.Error;
            var value = found.Value;
            return new HoldObjectResult(
                value.Path, value.Name, value.Point, held.Value.Hit, held.Value.Frames, held.Value.Seconds);
        }
    }
}
