using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using VSA.Presenters;

namespace VSA.Unity {
    // A uGUI view of one presenter. It only renders: it calls Render on Bind and on each Changed, on the main thread.
    // A factory gives the presenter with Bind. A view in a scene with no Bind before Start gets the presenter
    // from a new scope of the running app, and the view disposes that scope.
    public abstract class PresenterComponent<T> : MonoBehaviour where T : Presenter {
        IServiceScope scope;

        public T Presenter { get; private set; }

        // Listens to this presenter, stops to listen to the last one, and renders one time.
        public void Bind(T presenter) {
            if (ReferenceEquals(Presenter, presenter)) {
                return;
            }
            Unbind();
            Presenter = presenter;
            if (presenter != null) {
                presenter.Changed += OnChanged;
                Render();
            }
        }

        // Shows the state of the presenter. It runs on the main thread.
        protected abstract void Render();

        protected virtual void Start() {
            if (Presenter != null || VsaHost.Services == null) {
                return;
            }
            scope = VsaHost.Services.CreateScope();
            Bind(scope.ServiceProvider.GetRequiredService<T>());
        }

        protected virtual void OnDestroy() {
            Unbind();
            scope?.Dispose();
            scope = null;
        }

        // Changed can come on any thread. After the switch, the view can be destroyed.
        async void OnChanged() {
            await Awaitable.MainThreadAsync();
            if (this != null && Presenter != null) {
                Render();
            }
        }

        void Unbind() {
            if (Presenter != null) {
                Presenter.Changed -= OnChanged;
            }
            Presenter = null;
        }
    }
}
