using Umpire.Unity;
using Umpire.Vsa;

namespace Probe {
    // The probe needs the hooks to count the starts and stops. An app with no hooks uses VsaShell.
    public sealed class ProbeShell : Shell<Vsa<UnityHost>> {
        public static int Starts { get; private set; }
        public static int Stops { get; private set; }

        protected override void OnStart() {
            Starts++;
            ProbeRunner.Line("on-start", "ok start " + Starts);
        }

        protected override void OnStop() {
            Stops++;
            ProbeRunner.Line("on-stop", "ok stop " + Stops);
        }
    }
}
