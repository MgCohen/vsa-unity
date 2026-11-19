using System.Threading;
using System.Threading.Tasks;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;

// GreeterHost gives this module to the app of the sample, so that app does not read the Sample module.
namespace Greeter {
    public sealed class GreeterModule : Module {
    }

    // The Unity side gives the greeting. The module has no Unity reference: the VContainer root scope
    // registers an implementation, and the app imports it.
    public interface IGreetingSource {
        string Greeting { get; }
    }

    public sealed record GreetRequest(string Name) : IRequest<GreetResponse>;

    public sealed record GreetResponse(string Text) : IResponse;

    public sealed record Greeted(string Text, int Count) : IntegrationEvent;

    sealed class GreetHandler : RequestHandler<GreetRequest, GreetResponse> {
        static int count;

        readonly IGreetingSource source;
        readonly IIntegrationEventPublisher events;

        public GreetHandler(IGreetingSource source, IIntegrationEventPublisher events) {
            this.source = source;
            this.events = events;
        }

        public override async Task<Result<GreetResponse>> HandleAsync(Envelope<GreetRequest> envelope) {
            var text = $"{source.Greeting}, {envelope.Message.Name}!";
            await events.PublishAsync(new Greeted(text, Interlocked.Increment(ref count)), envelope.Cancellation);
            return new GreetResponse(text);
        }
    }

    public sealed class GreeterPresenter : Presenter, IPresenterHandler<Greeted> {
        public GreeterPresenter(IRequestSender requests) : base(requests) {
        }

        public string Last { get; private set; } = "No greeting yet";
        public int Count { get; private set; }

        public Task<Result<GreetResponse>> GreetAsync(string name) => Requests.SendAsync(new GreetRequest(name));

        public Task<Result<Unit>> HandleAsync(Envelope<Greeted> envelope) {
            Last = envelope.Message.Text;
            Count = envelope.Message.Count;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}
