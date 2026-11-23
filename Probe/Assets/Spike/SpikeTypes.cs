using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;

// The types of the use cases, in neutral names. Each case has its own service types, so no case changes another case.
// The spike module is internal: the scan of the probe app finds public module classes only, so this assembly
// stays out of the probe app. Only SpikeChecks composes it.
namespace Spike {
    internal sealed class SpikeModule : Module {
        // Edge: a second registration of a class that the scan registers too.
        public override void Configure(IServiceCollection services) {
            services.AddSingleton<IShelf<Article>, Shelf<Article>>();
        }
    }

    // The type arguments. Article and Badge are classes. Score and Flag are structs. FooBar has no marker.
    public abstract class Key {
    }

    [AutoClose]
    public sealed class Article : Key {
    }

    public sealed class Badge : Key {
    }

    public sealed class FooBar {
    }

    public interface IScore {
        int Value { get; set; }
    }

    public interface IFlag {
    }

    [AutoClose]
    public struct Score : IScore {
        public int Value { get; set; }
    }

    [AutoClose]
    public struct Flag : IFlag {
    }

    // The call log of the pipeline cases.
    [AutoRegister(Lifetime.Singleton)]
    public sealed class Hits {
        readonly List<string> all = new List<string>();

        public void Add(string hit) {
            lock (all) {
                all.Add(hit);
            }
        }

        public string[] All() {
            lock (all) {
                return all.ToArray();
            }
        }
    }

    // R1: an open command handler over a nested command, which sends a second open command with a struct result.
    public sealed record Put<T>(int Delta) : ICommand<int> where T : struct, IScore;

    public sealed record Store<T>(T Value) : ICommand<T> where T : struct, IScore;

    public sealed class PutHandler<T> : CommandHandler<Put<T>, int> where T : struct, IScore {
        readonly ICommandSender commands;

        public PutHandler(ICommandSender commands) {
            this.commands = commands;
        }

        public override async Task<Result<int>> HandleAsync(IEnvelope<Put<T>> envelope) {
            var next = new T();
            next.Value = envelope.Message.Delta;
            var stored = await commands.SendAsync(new Store<T>(next), envelope.Cancellation);
            return stored.IsSuccess ? stored.Value.Value : (Result<int>)stored.Error;
        }
    }

    public sealed class StoreHandler<T> : CommandHandler<Store<T>, T> where T : struct, IScore {
        public override Task<Result<T>> HandleAsync(IEnvelope<Store<T>> envelope) =>
            Task.FromResult<Result<T>>(envelope.Message.Value);
    }

    // R2: an open primitive, filtered by its constraint. Score is a type argument, but it is not an IFlag.
    public sealed record AddTag<T>(int Entity) : ICommand<Unit> where T : struct, IFlag;

    public sealed class TagHandler<T> : CommandHandler<AddTag<T>, Unit> where T : struct, IFlag {
        readonly Hits hits;

        public TagHandler(Hits hits) {
            this.hits = hits;
        }

        public override Task<Result<Unit>> HandleAsync(IEnvelope<AddTag<T>> envelope) {
            hits.Add("tag " + typeof(T).Name);
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    // R3: a list of a non-generic service, from an instance and from a factory.
    public interface IThing {
    }

    public sealed class ThingA : IThing {
    }

    public sealed class ThingB : IThing {
    }

    public sealed class Consumer {
        public Consumer(IEnumerable<IThing> things) {
            foreach (var thing in things) {
                Count++;
            }
        }

        public int Count { get; }
    }

    // R4: a registry made with MakeGenericType and added as an instance. A subclass has two service types.
    public class Registry<TKey> where TKey : Key {
        public string Name => "Registry<" + typeof(TKey).Name + ">";
    }

    public sealed class BadgeRegistry : Registry<Badge> {
    }

    // T1: one event, an open handler and a closed handler.
    public sealed record Changed<T>(T Value) : IntegrationEvent;

    public sealed class LogChanged<T> : IntegrationEventHandler<Changed<T>> {
        readonly Hits hits;

        public LogChanged(Hits hits) {
            this.hits = hits;
        }

        public override Task<Result<Unit>> HandleAsync(IEnvelope<Changed<T>> envelope) {
            hits.Add("log " + typeof(T).Name);
            return Done;
        }
    }

    public sealed class ScoreCheck : IntegrationEventHandler<Changed<Score>> {
        readonly Hits hits;

        public ScoreCheck(Hits hits) {
            this.hits = hits;
        }

        public override Task<Result<Unit>> HandleAsync(IEnvelope<Changed<Score>> envelope) {
            hits.Add("check");
            return Done;
        }
    }

    // T2: a hook for each command that implements IAuditable. No open generic.
    public interface IAuditable : ICommand {
    }

    public sealed record Rename(string Name) : ICommand<string>, IAuditable;

    public sealed class RenameHandler : CommandHandler<Rename, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Rename> envelope) =>
            Task.FromResult<Result<string>>(envelope.Message.Name);
    }

    public sealed class AuditHook : BeforeCommandHook<IAuditable> {
        readonly Hits hits;

        public AuditHook(Hits hits) {
            this.hits = hits;
        }

        public override Task<IAuditable> BeforeAsync(IEnvelope<IAuditable> envelope) {
            hits.Add("audit " + envelope.Message.GetType().Name);
            return Task.FromResult(envelope.Message);
        }
    }

    // T3: an open validator over a nested command.
    public sealed class RangeValidator<T> : Validator<Put<T>> where T : struct, IScore {
        public override Error Validate(IEnvelope<Put<T>> envelope) =>
            envelope.Message.Delta > 100 ? new Error("spike.range", "The delta is more than 100.") : null;
    }

    // T4: a pool or a cache, one for each type argument, resolved by app code.
    [AutoRegister(Lifetime.Singleton)]
    public interface IBox<T> {
    }

    public sealed class Box<T> : IBox<T> {
        readonly T[] values = new T[1];

        public bool Empty => EqualityComparer<T>.Default.Equals(values[0], default);
    }

    // T5: all rules for one type: two open rules and one closed rule.
    [AutoRegister(Lifetime.Singleton)]
    public interface IRule<T> {
    }

    public sealed class BaseRule<T> : IRule<T> {
    }

    public sealed class CritRule<T> : IRule<T> {
    }

    public sealed class ArticleRule : IRule<Article> {
    }

    // N1: open classes that close into a list of a non-generic service.
    [AutoRegister(Lifetime.Singleton)]
    public interface IMeter {
    }

    public sealed class Meter<T> : IMeter where T : struct {
    }

    // Edge: the scan registers Shelf<Article>, and a module registers it again by hand.
    [AutoRegister(Lifetime.Singleton)]
    public interface IShelf<T> {
    }

    public sealed class Shelf<T> : IShelf<T> {
    }

    // A command sender for the plain containers, which have no pipeline. No check calls it.
    public sealed class NoSender : ICommandSender {
        public Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct) =>
            throw new NotSupportedException("The plain containers have no pipeline.");
    }
}
