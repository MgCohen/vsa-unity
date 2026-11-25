using System.Threading.Tasks;
using VSA.TestSuite.Features.Objects;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabMissingScript : TestScript
    {
        public override string Name => "lab_missing";

        public override async Task Run(ScriptContext script)
        {
            await script.Send(new FindObject("LabCanvas/LabButton"));
            await script.Send(new WaitForObject("Missing/Thing", ObjectState.Active, 1));
        }
    }
}
