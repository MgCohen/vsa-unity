using Umpire.Kernels;
using Umpire.Unity;
using Umpire.Vsa;

namespace Probe {
    // Only for the compose checks of ProbeRunner, which compose the app with no shell and no host run.
    public sealed class ProbeApp : App<Vsa<UnityHost>> {
    }
}
