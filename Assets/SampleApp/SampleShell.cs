using Umpire.Unity;
using Umpire.Vsa;
using UnityEngine;

namespace SampleApp {
    // The shell of the sample. Put it on one GameObject of the first scene.
    public sealed class SampleShell : Shell<Vsa<SampleHost>> {
        protected override void OnStop() => Debug.Log("CHECK stopped");
    }
}
