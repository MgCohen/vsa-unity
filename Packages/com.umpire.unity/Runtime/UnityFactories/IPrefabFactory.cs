using UnityEngine;

namespace Umpire.Unity.Factories {
    // Makes a copy of a prefab and binds each IBindable<T> behaviour of the copy, also inactive ones, before Start.
    // Awake of the copy runs before the bind. Register it with AddPrefabFactory.
    public interface IPrefabFactory<TComponent> where TComponent : Component {
        TComponent Create(TComponent prefab, Transform parent = null);
    }

    // Same, and the component of the prefab type binds with this argument (AddFactory<TArg, T> of its target).
    // The other behaviours of the copy bind with no argument.
    public interface IPrefabFactory<in TArg, TComponent> where TComponent : Component {
        TComponent Create(TComponent prefab, TArg arg, Transform parent = null);
    }
}
