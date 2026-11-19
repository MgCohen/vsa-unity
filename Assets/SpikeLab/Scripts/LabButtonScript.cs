using System.Threading.Tasks;
using VSA.TestSuite.Spike.Features.Objects;
using VSA.TestSuite.Spike.Features.Touch;
using VSA.TestSuite.Spike.Plugins.Session;

namespace SpikeLab
{
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
            var clicks = SpikeLabCounter.Clicks;
            script.Check(clicks == before + 2, $"clicks went from {before} to {clicks}, not +2");
        }
    }
}
