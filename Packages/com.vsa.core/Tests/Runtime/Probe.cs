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

    // The app class of the tests. The NUnit reference keeps it out of the app scan of VsaHost.
    public sealed class ProbeApp : VsaApp<ProbeApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).Modules(new ProbeModule());
    }
}
