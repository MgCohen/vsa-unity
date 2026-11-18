using System.Threading.Tasks;
using DiceGame.Features.Counter.Infrastructure;
using DiceGame.Plugins.Dice.Contracts.Commands;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;

namespace DiceGame.Features.Counter.Slices.Roll {
    // Sends RollDice to the dice plugin, then adds the value to the roll log.
    sealed class RollHandler : RequestHandler<RollRequest, RollResponse> {
        const int Sides = 6;

        readonly ICommandSender commands;
        readonly RollLog log;

        public RollHandler(ICommandSender commands, RollLog log) {
            this.commands = commands;
            this.log = log;
        }

        public override async Task<Result<RollResponse>> HandleAsync(Envelope<RollRequest> envelope) {
            var roll = await commands.SendAsync(new RollDice(Sides), envelope.Cancellation);
            if (!roll.IsSuccess) {
                return roll.Error;
            }
            var (count, last) = log.Add(roll.Value);
            return new RollResponse(last, count);
        }
    }
}
