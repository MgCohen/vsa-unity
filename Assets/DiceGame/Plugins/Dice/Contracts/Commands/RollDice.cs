using Umpire.Vsa.Contracts.Commands;

namespace DiceGame.Plugins.Dice.Contracts.Commands {
    // Rolls one die with the given number of sides. The result is from 1 to Sides.
    public sealed record RollDice(int Sides) : ICommand<int>;
}
