using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace SpikeLab
{
    /// <summary>The state of the spike lab scene, for the manual checks.</summary>
    public sealed class SpikeLabState
    {
        public int Clicks { get; set; }
        public int Frame { get; set; }
        public bool Playing { get; set; }
    }

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
