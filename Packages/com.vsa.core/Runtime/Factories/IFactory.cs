namespace Umpire.Factories {
    // Makes a T from one argument on each call. For more values, give one record as the argument.
    // The factory with no argument is the template one: Umpire.Kernels.IFactory<T>.
    public interface IFactory<in TArg, out T> {
        T Create(TArg arg);
    }
}
