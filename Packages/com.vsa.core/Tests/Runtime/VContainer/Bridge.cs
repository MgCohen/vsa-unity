using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
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

namespace VSA.Unity.VContainer.Tests {
    // A small module for the tests. The handler needs IGreeting, a service of the VContainer root scope.
    public sealed class BridgeModule : VSA.Module.Module {
        public override void Configure(ModuleBuilder module) {
        }
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
        public GreetPresenter(IBus bus) : base(bus) {
        }

        public string Last { get; private set; } = "";
        public bool Disposed { get; private set; }

        public Task<Result<GreetResponse>> GreetAsync(string name) => Bus.Send(new GreetRequest(name));

        public Task OnEventAsync(Greeted integrationEvent, CancellationToken ct) {
            Last = integrationEvent.Text;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    public sealed class BridgeApp : VsaApp<BridgeApp> {
        protected override VsaBuilder Builder(string[] args) =>
            VsaBuilder.Create(args).Modules(new BridgeModule()).UseUnityHost().UseVContainer();
    }

    // The app has no UseVContainer, so the imports cannot reach it.
    public sealed class NoBridgeApp : VsaApp<NoBridgeApp> {
        protected override VsaBuilder Builder(string[] args) =>
            VsaBuilder.Create(args).Modules(new BridgeModule()).UseUnityHost();
    }

    // The root scope of the tests: it registers and imports IGreeting.
    public sealed class BridgeScope : VsaLifetimeScope<BridgeApp> {
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

    public sealed class NoBridgeScope : VsaLifetimeScope<NoBridgeApp> {
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
            AppRanAtInject = VsaHost.IsRunning;
            Presenter = presenter;
        }
    }
}
