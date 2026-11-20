using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace SpikeLab
{
    public static class SpikeLabCommands
    {
        [CliCommand("spikelab.state", "Clicks of LabButton, frame count and Play Mode state")]
        public static Task<SpikeLabState> State() => Task.FromResult(new SpikeLabState
        {
            Clicks = SpikeLabCounter.Clicks,
            Frame = Time.frameCount,
            Playing = Application.isPlaying,
        });
    }
}
