using System.Threading.Tasks;
using VSA.TestSuite.Spike;

namespace SpikeLab
{
    /// <summary>A sample script that fails: it waits 1 s for an object that does not exist.</summary>
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
