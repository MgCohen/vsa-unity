using DiceGame.Features.Counter;
using DiceGame.Plugins.Dice;
using Umpire.Unity;

namespace DiceGame.App {
    // The Unity host of the dice game. It names the modules, so the app does not get the other modules of the project.
    public sealed class DiceHost : UnityHost {
        public DiceHost() : base(new DicePlugin(), new CounterModule()) {
        }
    }
}
