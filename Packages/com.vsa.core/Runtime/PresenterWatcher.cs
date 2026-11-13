using System;
using System.Linq;
using UnityEngine;
using VSA.Presenters;

namespace VSA.Unity {
    // The base of a uGUI view that shows one or more presenters. It only renders: it listens to Changed and calls
    // Render on the main thread. The presenters get their events by themselves, with or without a view.
    // A view that combines modules calls Watch with the presenter of each module:
    //   void Start() => Watch(hand, pool);
    public abstract class PresenterWatcher : MonoBehaviour {
        Presenter[] watched = Array.Empty<Presenter>();
        bool destroyed;

        // Listens to these presenters, stops to listen to the others, and renders one time.
        // The same presenters again change nothing.
        protected void Watch(params Presenter[] presenters) {
            if (watched.SequenceEqual(presenters)) {
                return;
            }
            Unwatch();
            watched = presenters.ToArray();
            foreach (var presenter in watched) {
                presenter.Changed += OnChanged;
            }
            Render();
        }

        // Shows the state of the presenters. It runs on the main thread.
        protected abstract void Render();

        protected virtual void OnDestroy() {
            destroyed = true;
            Unwatch();
        }

        // Changed can come on any thread.
        void OnChanged() => MainThread.Post(() => {
            if (!destroyed) {
                Render();
            }
        });

        void Unwatch() {
            foreach (var presenter in watched) {
                presenter.Changed -= OnChanged;
            }
            watched = Array.Empty<Presenter>();
        }
    }
}
