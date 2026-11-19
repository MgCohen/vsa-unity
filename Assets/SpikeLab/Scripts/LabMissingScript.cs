using System.Threading.Tasks;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
    public sealed class LabMissingScript : TestScript
    {
        public override string Name => "lab_missing";

        public override async Task Run(ScriptContext script)
        {
            await script.Send(new FindObject("LabCanvas/LabButton"));
            await script.Send(new WaitForObject("Missing/Thing", 1));
        }
    }
}
