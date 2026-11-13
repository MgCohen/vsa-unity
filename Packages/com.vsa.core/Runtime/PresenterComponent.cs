using Microsoft.Extensions.DependencyInjection;
using VSA.Presenters;

namespace VSA.Unity {
    // A uGUI view that shows one presenter. A factory gives the presenter with Bind.
    // A view in a scene with no Bind before Start gets the presenter from a new scope of the running app,
    // and the view disposes that scope. For a view of more than one presenter, inherit PresenterWatcher.
    public abstract class PresenterComponent<T> : PresenterWatcher where T : Presenter {
        IServiceScope scope;

        public T Presenter { get; private set; }

        public void Bind(T presenter) {
            Presenter = presenter;
            Watch(presenter);
        }

        protected virtual void Start() {
            if (Presenter != null || VsaHost.Services == null) {
                return;
            }
            scope = VsaHost.Services.CreateScope();
            Bind(scope.ServiceProvider.GetRequiredService<T>());
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            scope?.Dispose();
            scope = null;
        }
    }
}
