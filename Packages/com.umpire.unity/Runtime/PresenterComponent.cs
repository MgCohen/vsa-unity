using UnityEngine;
using Umpire.Vsa.Presenters;

namespace Umpire.Unity {
    // A uGUI view of one presenter. It only renders: it calls Render on Bind and on each Changed, on the main thread.
    // The view never resolves its presenter. The app binds the views of each loaded scene with the presenters of the
    // app scope, and a prefab factory or VContainer can call Bind before that.
    // The scope that gives the presenter owns it, so the destroy of the view does not dispose the presenter.
    public abstract class PresenterComponent<T> : View<T> where T : Presenter {
        public T Presenter => Target;

        // Shows the state of the presenter. It runs on the main thread.
        protected abstract void Render();

        protected override void OnBind() {
            Target.Changed += OnChanged;
            Render();
        }

        protected override void OnRelease() => Target.Changed -= OnChanged;

        // Changed can come on any thread. After the switch, the view can be destroyed.
        async void OnChanged() {
            await Awaitable.MainThreadAsync();
            if (this != null && Target != null) {
                Render();
            }
        }
    }
}
