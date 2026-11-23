using Umpire.Vsa.Contracts.Commands;

namespace DiceGame.Plugins.Dice.Contracts.Commands {
    public sealed record RollDice(int Sides) : ICommand<int>;
}
