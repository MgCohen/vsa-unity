using Umpire.Vsa;

namespace Umpire.Unity {
    // The shell of a VSA app with the Unity host and no hooks. Put it on one GameObject of the first scene.
    public sealed class VsaShell : Shell<Vsa<UnityHost>> {
    }
}
