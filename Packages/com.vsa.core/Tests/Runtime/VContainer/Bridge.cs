using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Presenters;
using UnityEngine;
using VContainer;

namespace System.Runtime.CompilerServices {
    static class IsExternalInit {
    }
}

namespace Umpire.Unity.VContainer.Tests {
    // A small module for the tests. The handler needs IGreeting, a service of the VContainer root scope.
    public sealed class BridgeModule : Module {
    }

    public interface IGreeting {
        string Text { get; }
    }

    public sealed class Greeting : IGreeting {
        public Greeting(string text) => Text = text;

        public string Text { get; }
    }

    public sealed record GreetRequest(string Name) : IRequest<GreetResponse>;

    public sealed record GreetResponse(string Text) : IResponse;

    public sealed record Greeted(string Text) : IntegrationEvent;

    sealed class GreetHandler : RequestHandler<GreetRequest, GreetResponse> {
        readonly IGreeting greeting;
        readonly IIntegrationEventPublisher events;

        public GreetHandler(IGreeting greeting, IIntegrationEventPublisher events) {
            this.greeting = greeting;
            this.events = events;
        }

        public override async Task<Result<GreetResponse>> HandleAsync(Envelope<GreetRequest> envelope) {
            var text = greeting.Text + " " + envelope.Message.Name;
            await events.PublishAsync(new Greeted(text), envelope.Cancellation);
            return new GreetResponse(text);
        }
    }

    public sealed class GreetPresenter : Presenter, IPresenterEvent<Greeted> {
        public GreetPresenter(IRequestSender requests) : base(requests) {
        }

        public string Last { get; private set; } = "";
        public bool Disposed { get; private set; }

        public Task<Result<GreetResponse>> GreetAsync(string name) => Requests.SendAsync(new GreetRequest(name));

        public Task OnEventAsync(Greeted integrationEvent, CancellationToken ct) {
            Last = integrationEvent.Text;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    // The Unity host with the bridge module. The module scan does not read a test assembly.
    public sealed class BridgeHost : UnityHost {
        public BridgeHost() : base(new BridgeModule()) {
        }
    }

    // A host that does not derive from UnityHost, so the imports cannot reach the app.
    public sealed class PlainHost : AppHost {
        public PlainHost() {
            Modules = new Module[] { new BridgeModule() };
        }
    }

    // A shell with the bridge module, for the case of two apps.
    public sealed class BridgeShell : Shell<Vsa<BridgeHost>> {
    }

    // The root scope of the tests: it registers and imports IGreeting.
    public sealed class BridgeScope : VsaLifetimeScope<Vsa<BridgeHost>> {
        public static List<GameObject> NextTargets;

        protected override void Awake() {
            autoInjectGameObjects = NextTargets;
            NextTargets = null;
            base.Awake();
        }

        protected override void ConfigureScope(IContainerBuilder builder) {
            builder.RegisterInstance<IGreeting>(new Greeting("hello"));
            builder.RegisterVsa<GreetPresenter>();
        }

        protected override void Import(VsaImports imports) => imports.Add<IGreeting>();
    }

    public sealed class PlainScope : VsaLifetimeScope<Vsa<PlainHost>> {
        protected override void ConfigureScope(IContainerBuilder builder) =>
            builder.RegisterInstance<IGreeting>(new Greeting("hello"));

        protected override void Import(VsaImports imports) => imports.Add<IGreeting>();
    }

    // A scene object that VContainer injects.
    public sealed class GreetView : MonoBehaviour {
        public GreetPresenter Presenter { get; private set; }
        public bool AppRanAtInject { get; private set; }

        [Inject]
        public void Construct(GreetPresenter presenter) {
            AppRanAtInject = Shell.AppIsRunning;
            Presenter = presenter;
        }
    }
}
