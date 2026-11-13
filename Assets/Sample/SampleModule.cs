using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Handlers.Commands;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;

// A small module in one asmdef, for the IL2CPP check. The module scan finds it by this metadata.
[assembly: AssemblyMetadata("VsaModule", "Sample")]

namespace Sample {
    public sealed class SampleModule : VSA.Module.Module {
        public override void Configure(ModuleBuilder module) {
        }
    }

    public sealed record PingRequest(string Text) : IRequest<PongResponse>;

    public sealed record PongResponse(string Text) : IResponse;

    public sealed record Ponged(string Text) : IntegrationEvent;

    sealed class PingHandler : RequestHandler<PingRequest, PongResponse> {
        readonly IIntegrationEventPublisher events;

        public PingHandler(IIntegrationEventPublisher events) => this.events = events;

        public override async Task<Result<PongResponse>> HandleAsync(Envelope<PingRequest> envelope) {
            await events.PublishAsync(new Ponged(envelope.Message.Text), envelope.Cancellation);
            return new PongResponse("pong " + envelope.Message.Text);
        }
    }

    // A struct type argument closes the open handler: no AOT instance of MoveHandler<Deck> is in the code.
    public interface IZone : ITypeArgument {
        string Name { get; }
    }

    public readonly struct Hand : IZone {
        public string Name => "Hand";
    }

    public readonly struct Deck : IZone {
        public string Name => "Deck";
    }

    public sealed class Move<TZone> : ICommand<string> where TZone : struct, IZone {
        public Move(int card) => Card = card;

        public int Card { get; }
    }

    sealed class MoveHandler<TZone> : CommandHandler<Move<TZone>, string> where TZone : struct, IZone {
        public override Task<Result<string>> HandleAsync(Envelope<Move<TZone>> envelope) =>
            Task.FromResult<Result<string>>("moved card " + envelope.Message.Card + " to " + default(TZone).Name);
    }

    public sealed class PingPresenter : Presenter, IPresenterEvent<Ponged> {
        public PingPresenter(IBus bus) : base(bus) {
        }

        public string Last { get; private set; } = "";
        public int EventThread { get; private set; } = -1;

        public Task<Result<PongResponse>> PingAsync(string text) => Bus.Send(new PingRequest(text));

        public Task OnEventAsync(Ponged integrationEvent, CancellationToken ct) {
            Last = integrationEvent.Text;
            EventThread = Thread.CurrentThread.ManagedThreadId;
            return Task.CompletedTask;
        }
    }
}

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}
