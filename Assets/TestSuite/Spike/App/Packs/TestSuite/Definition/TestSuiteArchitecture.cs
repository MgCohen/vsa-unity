using Umpire.Architecture;
using Umpire.Kernels.Blocks;
using Umpire.Vsa.Definition;

namespace VSA.TestSuite.Spike.Packs.TestSuite
{
    public sealed class TestSuiteArchitecture : VsaArchitecture
    {
        protected override void Define(Part root, Part kernel)
        {
            root.For<Module>().Add<CliCommand>();
        }

        protected override void Rules(RuleBook rules)
        {
            rules.For<CliCommand>().Uses<Contracts>().And<Contracts.Requests>().And<Infrastructure>()
                .And<Domain.Shared>().And<Slice.Shared>();
            rules.For<Module>().Uses<Module>(Depth.Self).InSame<Module>();
        }
    }
}
