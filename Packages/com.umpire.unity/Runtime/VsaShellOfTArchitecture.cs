using Umpire.Vsa;
using Umpire.Vsa.Definition;

namespace Umpire.Unity {
    // The shell of a VSA app with the Unity host, for an app with hooks or its own architecture. The app does not
    // name the host: public sealed class GameShell : VsaShell<VsaArchitecture> { protected override void OnStart() { } }
    public abstract class VsaShell<TArchitecture> : Shell<Vsa<TArchitecture, UnityHost>>
        where TArchitecture : VsaArchitecture, new() {
    }
}
