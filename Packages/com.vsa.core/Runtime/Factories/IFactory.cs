namespace Umpire.Factories {
    // Makes a T on each call. Register it with AddFactory. Inject the factory, not the container.
    public interface IFactory<out T> {
        T Create();
    }

    // Makes a T from one argument on each call. For more values, give one record as the argument.
    public interface IFactory<in TArg, out T> {
        T Create(TArg arg);
    }
}
