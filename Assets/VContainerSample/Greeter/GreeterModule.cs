using System.Threading;
using System.Threading.Tasks;
using VSA.Contracts;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;

// No VsaModule metadata: VContainerSampleApp gives this module with Modules(...), so the module scan of
// other apps (SampleApp) does not find it.
namespace Greeter {
    public sealed class GreeterModule : VSA.Module.Module {
        public override void Configure(ModuleBuilder module) {
        }
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

    public sealed class GreeterPresenter : Presenter, IPresenterEvent<Greeted> {
        public GreeterPresenter(IBus bus) : base(bus) {
        }

        public string Last { get; private set; } = "No greeting yet";
        public int Count { get; private set; }

        public Task<Result<GreetResponse>> GreetAsync(string name) => Bus.Send(new GreetRequest(name));

        public Task OnEventAsync(Greeted integrationEvent, CancellationToken ct) {
            Last = integrationEvent.Text;
            Count = integrationEvent.Count;
            return Task.CompletedTask;
        }
    }
}

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}
