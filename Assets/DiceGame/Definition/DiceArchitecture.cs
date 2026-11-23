using Umpire.Architecture;
using Umpire.Vsa.Definition;
using AppRoot = Umpire.Architecture.Blocks.App;
using KernelParts = Umpire.Kernels.KernelParts;
using Layers = Umpire.Vsa.Definition;

namespace DiceGame.App {
    // The VSA rules, and the Unity view bases for the views. Without UnityViews and Factories, the engine binds
    // Umpire.Unity.dll and Umpire.Factories.dll to the Kernel part, and the views cannot use them. DiceViewsModule
    // in Views registers the views, so Views use Module and the host uses Views. The host and the app class
    // (DiceShell) use UnityViews for UnityHost and Shell. The engine calls the Rules of VsaArchitecture first, so
    // this method does not call base.Rules.
    public sealed class DiceArchitecture : VsaArchitecture {
        protected override void Rules(RuleBook rules) {
            rules.For<Layers.Views>().Uses<UnityViews>().And<Factories>();
            rules.For<Layers.Views>().Uses<KernelParts.Modules>();
            rules.For<Host>().And<AppRoot>(Depth.Self).Uses<UnityViews>();
            rules.For<Host>().Uses<Layers.Views>();
        }
    }
}
