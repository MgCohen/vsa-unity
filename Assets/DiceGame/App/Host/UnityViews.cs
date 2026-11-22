using Umpire.Architecture;

namespace DiceGame.App {
    // The view bases and the prefab factories of the Unity package. The longest matching DLL name wins over Kernel.
    public sealed record UnityViews : External {
        public override string Assembly => "Umpire.Unity";
    }
}
