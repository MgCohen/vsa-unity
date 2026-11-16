using System.Threading;
using System.Threading.Tasks;
using VSA.Composition;
using VSA.Contracts;
using VSA.Contracts.Events;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}

namespace VSA.Unity.Tests {
    // A small module for the tests. The app gives it with Modules(...), so the module scan does not find it.
    public sealed class ProbeModule : VSA.Module.Module {
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

    public sealed class PingPresenter : Presenter, IPresenterEvent<Ponged> {
        public PingPresenter(IBus bus) : base(bus) {
        }

        public string Last { get; private set; } = "";
        public int EventThread { get; private set; } = -1;
        public bool Disposed { get; private set; }

        public Task PingAsync(string text) => Bus.Send(new PingRequest(text));

        // A change of this presenter only: Run raises Changed two times, and no event goes to the other presenters.
        public Task TouchAsync() => Run(new OpState(), () => Task.FromResult<Error>(null));

        public Task OnEventAsync(Ponged integrationEvent, CancellationToken ct) {
            Last = integrationEvent.Text;
            EventThread = Thread.CurrentThread.ManagedThreadId;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    // The app class of the tests. It selects the Unity host, so the host scan does not run.
    public sealed class ProbeApp : VsaApp<ProbeApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).Modules(new ProbeModule()).UseUnityHost();
    }

    // The host component of the tests.
    public sealed class ProbeHost : VsaHost<ProbeApp> {
    }
}
