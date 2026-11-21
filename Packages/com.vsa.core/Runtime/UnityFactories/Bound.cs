using Umpire.Factories;
using UnityEngine;

namespace Umpire.Unity.Factories {
    // A MonoBehaviour that binds to one .NET object, for example a view controller or a character model.
    // A prefab factory or the scene injection calls Bind before Start. The behaviour never resolves its target.
    // An override of OnDestroy calls the base method.
    public abstract class Bound<T> : MonoBehaviour, IBindable<T> where T : class {
        public T Target { get; private set; }

        // Releases the last target, then binds to this one. The same target again changes nothing.
        public void Bind(T target) {
            if (ReferenceEquals(Target, target)) {
                return;
            }
            Release();
            Target = target;
            if (target != null) {
                OnBind();
            }
        }

        // Runs after each Bind to a new target.
        protected virtual void OnBind() {
        }

        // Runs when the behaviour stops to use its target: at a Bind to another target, and at the destroy.
        protected virtual void OnRelease() {
        }

        protected virtual void OnDestroy() => Release();

        void Release() {
            if (Target == null) {
                return;
            }
            OnRelease();
            Target = null;
        }
    }
}
