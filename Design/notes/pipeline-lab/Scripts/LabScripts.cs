using System.Threading.Tasks;
using VSA.TestSuite;

// C# test scripts for the lab. Run them with "suite.run --script lab_click".
[UnityEngine.Scripting.Preserve]
sealed class LabClickScript : ITestScript
{
    public string Name => "lab_click";

    public async Task Run(ScriptContext s)
    {
        await s.Reset("SampleScene", seed: 1);
        LabUi.Create();
        await s.WaitForObject("LabCanvas/LabButton");
        int before = LabCounter.Clicks;
        await s.ClickObject("LabCanvas/LabButton");
        await s.ClickObject("LabCanvas/LabButton");
        await s.WaitFrames(1);
        await s.AssertText("LabCanvas/LabLabel", "clicks=" + (before + 2));
    }
}

[UnityEngine.Scripting.Preserve]
sealed class LabFailScript : ITestScript
{
    public string Name => "lab_fail";

    public async Task Run(ScriptContext s)
    {
        await s.Reset("SampleScene");
        LabUi.Create();
        await s.WaitFrames(1);
        await s.AssertText("LabCanvas/LabLabel", "this text is wrong");
        await s.ClickObject("LabCanvas/LabButton");   // never runs
    }
}

// A slow script with visible steps, for the video recording demo.
[UnityEngine.Scripting.Preserve]
sealed class LabDemoScript : ITestScript
{
    public string Name => "lab_demo";

    public async Task Run(ScriptContext s)
    {
        await s.Reset("SampleScene", seed: 1);
        LabUi.Create();
        LabSuiteCommands.AddDragTargets(UnityEngine.GameObject.Find("LabCanvas"));
        await s.WaitForObject("LabCanvas/LabButton");
        int before = LabCounter.Clicks;
        for (int i = 0; i < 3; i++)
        {
            await s.WaitSeconds(0.4);
            await s.ClickObject("LabCanvas/LabButton");
        }
        await s.WaitSeconds(0.4);
        await s.DragObject("LabCanvas/LabDrag", "LabCanvas/LabDrop", steps: 40);
        await s.WaitSeconds(0.4);
        await s.AssertText("LabCanvas/LabLabel", "clicks=" + (before + 3));
    }
}

// A script that waits longer than the command time limit, to test that the recording still stops.
[UnityEngine.Scripting.Preserve]
sealed class LabHangScript : ITestScript
{
    public string Name => "lab_hang";

    public async Task Run(ScriptContext s)
    {
        await s.Reset("SampleScene");
        LabUi.Create();
        await s.WaitSeconds(60);
    }
}
