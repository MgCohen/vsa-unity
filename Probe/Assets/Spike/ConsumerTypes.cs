using System.Collections.Generic;
using System.Linq;

namespace Spike {
    // C1 to C3: a consumer module registers open types by hand, with no Umpire attribute.
    // Coin is a struct with no [AutoClose], so only the constructor walk can find it.
    public struct Coin : IScore {
        public int Value { get; set; }
    }

    public interface IStock<T> {
    }

    public sealed class Stock<T> : IStock<T> {
    }

    // C2: a nested shape. Entry<Coin> implements IEntry<Put<Coin>>.
    public interface IEntry<T> {
    }

    public sealed class Entry<T> : IEntry<Put<T>> where T : struct, IScore {
    }

    // C1: a simple shape over a struct and over a class.
    public sealed class Till {
        public Till(IStock<Coin> coins, IStock<Article> articles) {
            Coins = coins;
            Articles = articles;
        }

        public IStock<Coin> Coins { get; }
        public IStock<Article> Articles { get; }
    }

    public sealed class Ledger {
        public Ledger(IEntry<Put<Coin>> entry) {
            Entry = entry;
        }

        public IEntry<Put<Coin>> Entry { get; }
    }

    // C3: a list. The module registers IStock<Article> closed, and also open.
    public sealed class Shelves {
        public Shelves(IEnumerable<IStock<Article>> stocks) {
            Count = stocks.Count();
        }

        public int Count { get; }
    }
}
