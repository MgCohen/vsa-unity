using Umpire.Vsa;

namespace Umpire.Unity {
    // The shell of a VSA app with the Unity host and no hooks. Put it on one GameObject of the first scene.
    // Not sealed: the rule engine finds the app class only in the app code, not in a package from a git URL.
    // So an app derives its own shell in Assets: public sealed class GameShell : VsaShell { }
    public class VsaShell : Shell<Vsa<UnityHost>> {
    }
}
