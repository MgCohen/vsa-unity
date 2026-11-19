using System.Collections.Concurrent;
using DiceGame.Features.Counter.Domain;

namespace DiceGame.Features.Counter.Infrastructure {
    sealed class RollLogStore {
        internal ConcurrentDictionary<int, RollLog> Logs { get; } = new ConcurrentDictionary<int, RollLog>();
    }
}
