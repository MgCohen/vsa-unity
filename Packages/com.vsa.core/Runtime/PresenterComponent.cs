using System;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using Umpire.Vsa.Presenters;

namespace Umpire.Unity {
    // A uGUI view of one presenter. It only renders: it calls Render on Bind and on each Changed, on the main thread.
    // The view never resolves its presenter. It gets it from Bind: the app injects the views of each loaded scene
    // with the presenters of the app scope, and a factory or VContainer can call Bind before that.
    // The scope that gives the presenter owns it, so the destroy of the view does not dispose the presenter.
    public abstract class PresenterComponent<T> : MonoBehaviour, IPresenterView where T : Presenter {
        public T Presenter { get; private set; }

        Presenter IPresenterView.Presenter => Presenter;

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

        void IPresenterView.Bind(IServiceProvider presenters) => Bind(presenters.GetRequiredService<T>());

        void IPresenterView.Release(Presenter presenter) {
            if (ReferenceEquals(Presenter, presenter)) {
                Unbind();
            }
        }

        // Shows the state of the presenter. It runs on the main thread.
        protected abstract void Render();

        protected virtual void OnDestroy() => Unbind();

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

    // A view that the app can inject. PresenterComponent<T> is the only one.
    interface IPresenterView {
        Presenter Presenter { get; }

        void Bind(IServiceProvider presenters);

        // Stops to listen to this presenter, if the view still shows it. The app calls it at its stop, so a new
        // start can inject the view again.
        void Release(Presenter presenter);
    }
}
