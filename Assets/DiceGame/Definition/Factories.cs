using Umpire.Architecture;

namespace DiceGame.App {
    // The factories and the bind step: IFactory<TArg, T> and IBindable<T>.
    public sealed record Factories : External {
        public override string Assembly => "Umpire.Factories";
    }
}
