using System;
using System.Threading.Tasks;
using DiceGame.Plugins.Dice.Contracts.Commands;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace DiceGame.Plugins.Dice.Handlers.Commands {
    sealed class RollDiceHandler : CommandHandler<RollDice, int> {
        static readonly Random Random = new Random();

        public override Task<Result<int>> HandleAsync(IEnvelope<RollDice> envelope) {
            lock (Random) {
                return Task.FromResult<Result<int>>(Random.Next(1, envelope.Message.Sides + 1));
            }
        }
    }
}
