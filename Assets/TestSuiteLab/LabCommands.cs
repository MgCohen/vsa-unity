using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace TestSuiteLab
{
    public static class LabCommands
    {
        [CliCommand("testsuite_lab.state", "Clicks of LabButton, frame count and Play Mode state")]
        public static Task<LabState> State() => Task.FromResult(new LabState
        {
            Clicks = LabCounter.Clicks,
            Frame = Time.frameCount,
            Playing = Application.isPlaying,
        });
    }
}
