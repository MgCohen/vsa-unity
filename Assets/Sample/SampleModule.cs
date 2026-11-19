using System.Threading;
using System.Threading.Tasks;
using Umpire.Kernels;
using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;

// A small module in one asmdef, for the IL2CPP check. The module name is the last part of the namespace: Sample.
namespace Sample {
    public sealed class SampleModule : Module {
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
    public interface IZone {
        string Name { get; }
    }

    [TypeArgument]
    public readonly struct Hand : IZone {
        public string Name => "Hand";
    }

    [TypeArgument]
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

    public sealed class PingPresenter : Presenter, IPresenterHandler<Ponged> {
        public PingPresenter(IRequestSender requests) : base(requests) {
        }

        public string Last { get; private set; } = "";
        public int EventThread { get; private set; } = -1;

        public Task<Result<PongResponse>> PingAsync(string text) => Requests.SendAsync(new PingRequest(text));

        public Task<Result<Unit>> HandleAsync(Envelope<Ponged> envelope) {
            Last = envelope.Message.Text;
            EventThread = Thread.CurrentThread.ManagedThreadId;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}
