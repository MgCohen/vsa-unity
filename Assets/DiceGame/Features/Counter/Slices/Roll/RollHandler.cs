using System.Threading.Tasks;
using DiceGame.Features.Counter.Domain;
using DiceGame.Plugins.Dice.Contracts.Commands;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

namespace DiceGame.Features.Counter.Slices.Roll {
    sealed class RollHandler : RequestHandler<RollRequest, RollResponse> {
        const int Sides = 6;

        readonly ICommandSender commands;
        readonly IRepository<RollLog, int> logs;

        public RollHandler(ICommandSender commands, IRepository<RollLog, int> logs) {
            this.commands = commands;
            this.logs = logs;
        }

        public override async Task<Result<RollResponse>> HandleAsync(Envelope<RollRequest> envelope) {
            var roll = await commands.SendAsync(new RollDice(Sides), envelope.Cancellation);
            if (!roll.IsSuccess) {
                return roll.Error;
            }
            var log = await logs.GetAsync(RollLog.OnlyId, envelope.Cancellation) ?? RollLog.Start();
            log.Add(roll.Value);
            logs.Add(log);
            await logs.SaveChangesAsync(envelope.Cancellation);
            return new RollResponse(log.Last, log.Count);
        }
    }
}
