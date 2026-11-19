using Umpire.Unity;
using Umpire.Vsa;
using UnityEngine;

namespace SampleApp {
    public sealed class SampleShell : Shell<Vsa<SampleHost>> {
        protected override void OnStop() => Debug.Log("CHECK stopped");
    }
}
