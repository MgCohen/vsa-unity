using System.Collections.Generic;
using System.Threading.Tasks;
using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

[assembly: AutoClose(typeof(int))]

namespace Probe.Lab {
    // The type arguments. Umpire closes each open [AutoRegister] class over each of them (MakeGenericType).
    [AutoClose]
    public sealed class Article {
    }

    [AutoClose]
    public struct Score {
        public int Value;
    }

    // The control: a struct like Score, but Mention() below names Store<Mentioned> in code.
    [AutoClose]
    public struct Mentioned {
        public int Value;
    }

    public interface IStore {
        string Roundtrip();
    }

    // An open class. No code names a closed form of it, except Mention().
    [AutoRegister(Lifetime.Singleton)]
    public sealed class Store<T> : IStore {
        readonly List<T> items = new List<T>();

        public string Roundtrip() {
            items.Add(default);
            var index = new HashSet<T> { items[0] };
            return typeof(T).Name + " " + index.Count + " " + EqualityComparer<T>.Default.Equals(items[0], default);
        }
    }

    // A generic event and an open handler. The code publishes Changed<T>, but no code names ChangedHandler<T>.
    public sealed record Changed<T>(T Value) : IntegrationEvent;

    public sealed class ChangedHandler<T> : IntegrationEventHandler<Changed<T>> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<Changed<T>> envelope) {
            lock (Hits) {
                Hits.Add(typeof(T).Name + "=" + envelope.Message.Value);
            }
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }

        public static List<string> Hits => ChangedHits.All;
    }

    public static class ChangedHits {
        public static readonly List<string> All = new List<string>();
    }

    static class Mentions {
        // Never called. It only makes IL2CPP generate the code of Store<Mentioned> and ChangedHandler<Mentioned>.
        static void Mention() {
            new Store<Mentioned>().Roundtrip();
            new ChangedHandler<Mentioned>().HandleAsync(null);
        }
    }

    // An open service interface with [AutoRegister]. Umpire closes Shelf<T> and registers IShelf<T> for each type argument.
    [AutoRegister(Lifetime.Singleton)]
    public interface IShelf<T> {
        string Name { get; }
    }

    public sealed class Shelf<T> : IShelf<T> {
        public string Name => typeof(T).Name;
    }

    // A class with no [AutoClose]. Umpire does not close Shelf<T> over it.
    public sealed class FooBar {
    }

    // Open generics in Microsoft.Extensions.DependencyInjection itself, with no Umpire code.
    public interface IBox<T> {
        string Name { get; }
    }

    public sealed class Box<T> : IBox<T> {
        readonly T[] values = new T[1];

        public string Name => typeof(T).Name + " " + values.Length + " " + EqualityComparer<T>.Default.Equals(values[0], default);
    }

    // An open service with two type arguments.
    public interface IPair<TFirst, TSecond> {
        string Name { get; }
    }

    public sealed class Pair<TFirst, TSecond> : IPair<TFirst, TSecond> {
        public string Name => typeof(TFirst).Name + "+" + typeof(TSecond).Name;
    }

    // A struct that no code names in a closed generic. Only MakeGenericType closes Box<> over it.
    public struct Loose {
        public int Value;
    }
}
