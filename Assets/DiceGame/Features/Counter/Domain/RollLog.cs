using Umpire.Vsa.Domain;

namespace DiceGame.Features.Counter.Domain {
    public sealed class RollLog : Entity<int> {
        public const int OnlyId = 1;

        RollLog() {
        }

        public int Count { get; private set; }

        public int Last { get; private set; }

        public static RollLog Start() => new RollLog { Id = OnlyId };

        public void Add(int value) {
            Count++;
            Last = value;
        }
    }
}
