using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace VSA.TestSuite.Spike.Packs.TestSuite
{
    public sealed class TestSuiteArchitecture : VsaArchitecture
    {
        protected override void DefinePlugin(PartGroup plugin)
        {
            base.DefinePlugin(plugin);
            plugin.Add<CliCommand>();
        }

        protected override void DefineFeature(PartGroup feature)
        {
            base.DefineFeature(feature);
            feature.Add<CliCommand>();
        }

        protected override void Rules(RuleBook rules)
        {
            base.Rules(rules);
            rules.For<CliCommand>().Uses<Contracts>().And<Contracts.Requests>().And<Infrastructure>();
        }
    }
}
