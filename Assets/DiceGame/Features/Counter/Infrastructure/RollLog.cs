using Umpire.Markers;

namespace DiceGame.Features.Counter.Infrastructure {
    // The rolls of the app: the number of rolls and the last value.
    [AutoRegister(Lifetime.Singleton)]
    sealed class RollLog {
        readonly object gate = new object();

        public int Count { get; private set; }

        public int Last { get; private set; }

        public (int Count, int Last) Add(int value) {
            lock (gate) {
                Count++;
                Last = value;
                return (Count, Last);
            }
        }
    }
}
