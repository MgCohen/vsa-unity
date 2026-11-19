using DiceGame.Features.Counter;
using DiceGame.Plugins.Dice;
using Umpire.Unity;

namespace DiceGame.App {
    public sealed class DiceHost : UnityHost {
        public DiceHost() : base(new DicePlugin(), new CounterModule()) {
        }
    }
}
