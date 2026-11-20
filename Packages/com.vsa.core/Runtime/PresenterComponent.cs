using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using Umpire.Vsa.Presenters;

namespace Umpire.Unity {
    // A uGUI view of one presenter. It only renders: it calls Render on Bind and on each Changed, on the main thread.
    // A factory gives the presenter with Bind, and the scope of that presenter must be a live PresenterScope.
    // A view in a scene with no Bind before Start makes a new scope of the running app, makes it a live
    // PresenterScope, gets the presenter from it, and disposes that scope with the view.
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
            if (Presenter != null || Shell.Services == null) {
                return;
            }
            scope = Shell.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PresenterScope>();
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
