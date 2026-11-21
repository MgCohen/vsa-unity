namespace Umpire.Factories {
    // An object that binds to one T. When a factory makes it, the factory gets the T and calls Bind.
    // The T comes from the factory of T if T has one. Else the container resolves T, for example a scoped presenter.
    public interface IBindable<in T> {
        void Bind(T target);
    }

    // Binds an object that a factory did not make, for example a copy of a Unity prefab.
    // If the object is not IBindable<T>, Bind does nothing.
    public interface IBinder {
        void Bind(object product);
    }
}
