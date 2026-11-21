using System.Threading;
using System.Threading.Tasks;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Definition;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}

namespace Umpire.Unity.Tests {
    // A small module for the tests. The module scan does not read a test assembly, so ProbeHost gives the module.
    public sealed class ProbeModule : Module<ProbeSettings> {
        public static ProbeSettings Bound { get; private set; }

        public override void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services, ProbeSettings settings) {
            Bound = settings;
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<PingController>(services);
            // One FreshController for each view: the module registration wins over the scan of the scene targets.
            Umpire.Factories.FactoryRegistrationExtensions.AddFactory<FreshController>(services);
        }
    }

    // A controller with no registration. The scan of the scene targets registers it as scoped.
    public sealed class NoteController {
        public NoteController(PingPresenter presenter) => Presenter = presenter;

        public PingPresenter Presenter { get; }
    }

    // A controller that the module registers with a factory.
    public sealed class FreshController {
    }

    // A view controller: a plain class that the app scope makes, with the presenter of the app scope.
    public sealed class PingController {
        public PingController(PingPresenter presenter) => Presenter = presenter;

        public PingPresenter Presenter { get; }
    }

    public sealed class ProbeSettings {
        public string Text { get; set; } = "default";
    }

    // The settings of ProbeModule in code. UnityHost finds the class in the assembly of the module.
    public sealed class CodeSettings : UnitySettings {
        public override TSettings Get<TSettings>(Module module) =>
            module is ProbeModule ? new ProbeSettings { Text = "code" } as TSettings : null;
    }

    public sealed record PingRequest(string Text) : IRequest<PongResponse>;

    public sealed record PongResponse(string Text) : IResponse;

    public sealed record Ponged(string Text) : IntegrationEvent;

    sealed class PingHandler : RequestHandler<PingRequest, PongResponse> {
        readonly IIntegrationEventPublisher events;

        public PingHandler(IIntegrationEventPublisher events) => this.events = events;

        public override async Task<Result<PongResponse>> HandleAsync(IEnvelope<PingRequest> envelope) {
            await events.PublishAsync(new Ponged(envelope.Message.Text), envelope.Cancellation);
            return new PongResponse("pong " + envelope.Message.Text);
        }
    }

    public sealed class PingPresenter : Presenter, IPresenterHandler<Ponged> {
        public PingPresenter(IRequestSender requests) : base(requests) {
        }

        public string Last { get; private set; } = "";
        public int EventThread { get; private set; } = -1;
        public bool Disposed { get; private set; }

        public Task<Result<PongResponse>> PingAsync(string text) => Requests.SendAsync(new PingRequest(text));

        // A change of this presenter only: Run raises Changed two times, and no event goes to the other presenters.
        public Task TouchAsync() => Run(new OpState(), () => Task.FromResult<Error>(null));

        public Task<Result<Unit>> HandleAsync(IEnvelope<Ponged> envelope) {
            Last = envelope.Message.Text;
            EventThread = Thread.CurrentThread.ManagedThreadId;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }

        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    // The host of the tests: the Unity host with the probe module.
    public sealed class ProbeHost : UnityHost {
        public ProbeHost() : base(new ProbeModule()) {
        }
    }

    // The shell of the tests. It counts the app hooks.
    public sealed class ProbeShell : Shell<Vsa<ProbeHost>> {
        public static int Starts;
        public static int Stops;

        protected override void OnStart() => Starts++;

        protected override void OnStop() => Stops++;
    }

    // A shell with hooks that does not name the host. UnityHost has no module list here, so Umpire scans the
    // loaded assemblies of the project for modules.
    public sealed class HookShell : VsaShell<VsaArchitecture> {
        public static int Starts;

        protected override void OnStart() => Starts++;
    }
}
