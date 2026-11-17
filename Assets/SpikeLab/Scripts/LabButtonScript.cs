using System.Threading.Tasks;
using VSA.TestSuite.Spike;

namespace SpikeLab
{
    /// <summary>A sample test script: wait for the lab button, touch it two times, check the clicks.</summary>
    public sealed class LabButtonScript : TestScript
    {
        public override string Name => "lab_button";

        public override async Task Run(ScriptContext script)
        {
            var before = SpikeLabCounter.Clicks;
            await script.Send(new WaitForObject("LabCanvas/LabButton", 2));
            var first = await script.Send(new TouchObject("LabCanvas/LabButton"));
            script.Check(first.Hit == "LabButton", $"the first touch hit {first.Hit}");
            await script.Send(new TouchScreen(first.Point));
            script.Check(SpikeLabCounter.Clicks == before + 2, $"clicks went from {before} to {SpikeLabCounter.Clicks}, not +2");
        }
    }
}
