using System.Threading.Tasks;
using VSA.TestSuite.Features.Wait;
using VSA.TestSuite.Plugins.Session;

namespace TestSuiteLab
{
    public sealed class LabWaitScript : TestScript
    {
        public override string Name => "lab_wait";

        public override async Task Run(ScriptContext script)
        {
            var frames = await script.Send(new WaitFrames(3));
            script.Check(frames.Frames == 3, $"wait.frames 3 took {frames.Frames} frames");
            var seconds = await script.Send(new WaitSeconds(0.25f));
            script.Check(seconds.Seconds >= 0.25f && seconds.Frames > 0,
                $"wait.seconds 0.25 took {seconds.Seconds} s and {seconds.Frames} frames");
        }
    }
}
