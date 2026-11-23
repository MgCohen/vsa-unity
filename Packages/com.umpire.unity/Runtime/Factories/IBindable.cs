namespace Umpire.Factories {
    // An object that binds to one T. When a factory makes it, the factory gets the T and calls Bind.
    // The T comes from the factory of T if T has one. Else the container resolves T, for example a scoped presenter.
    // Limit: if T also binds to the type of this object, each Bind makes the other one again, with no end.
    public interface IBindable<in T> {
        void Bind(T target);
    }

    // Binds an object that a factory did not make, for example a copy of a Unity prefab.
    // If the object is not IBindable<T>, Bind does nothing.
    public interface IBinder {
        // Gets the T from the factory of T, else from the scope.
        void Bind(object product);

        // Gets the T from the factory of T with this argument (AddFactory<TArg, T>).
        void Bind(object product, object arg);
    }
}
