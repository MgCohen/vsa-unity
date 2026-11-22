using Umpire.Architecture;
using Umpire.Vsa.Definition;
using Blocks = Umpire.Architecture.Blocks;
using Layers = Umpire.Vsa.Definition;

namespace DiceGame.App {
    // The VSA rules, and the Unity view bases for the views. Without UnityViews and Factories, the engine binds
    // Umpire.Unity.dll and Umpire.Factories.dll to the Kernel part, and the views cannot use them. The host uses
    // UnityViews for UnityHost and Shell.
    public sealed class DiceArchitecture : VsaArchitecture {
        protected override void Define(Block block) {
            base.Define(block);
            block.Add<UnityViews>();
            block.Add<Factories>();
        }

        protected override void Rules(RuleBook rules) {
            base.Rules(rules);
            rules.For<Layers.Views>().Uses<UnityViews>().And<Factories>();
            rules.For<Layers.Views>().Under<Blocks.App>().Uses<Blocks.Kernel>(Depth.Self);
            rules.For<Host>().Under<Blocks.App>().Uses<Layers.Views>();
            rules.For<Host>().Uses<UnityViews>();
        }
    }
}
