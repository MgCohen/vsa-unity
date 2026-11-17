namespace StatusLab
{
    /// <summary>One signal. Add a source: one file in Sources/ and one entry in Sampler.</summary>
    public interface ISignalSource
    {
        string Key { get; }

        /// <summary>Reads the signal into the snapshot. Never throws for a missing Editor.</summary>
        Task Read(Snapshot into, Target target, CancellationToken cancel);
    }
}
