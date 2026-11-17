namespace SpikeLab
{
    /// <summary>The state of the spike lab scene, for the manual checks.</summary>
    public sealed class SpikeLabState
    {
        public int Clicks { get; set; }
        public int Frame { get; set; }
        public bool Playing { get; set; }
    }
}
