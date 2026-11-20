namespace VSA.TestSuite.Client.Status
{
    /// <summary>Runs all signal sources, one after another, so only one request to the Editor is in flight.</summary>
    public sealed class Sampler
    {
        readonly IClock clock;

        public List<ISignalSource> Sources { get; } = new()
        {
            new ProcessSource(),
            new DescriptorSource(),
            new StatusSource(),
            new ProgressSource(),
            new WindowSource(),
            new LogSource(),
            new CrashSource(),
            new BeaconSource(),
            new ProbeSource(),
            new TruthSource(),
            new UnityStatusSource(),
            new EditorsSource(),
        };

        public Sampler(IClock clock) => this.clock = clock;

        public async Task<Snapshot> Read(Target target, CancellationToken cancel)
        {
            var snapshot = new Snapshot { T = clock.NowMs };
            foreach (var source in Sources)
            {
                try { await source.Read(snapshot, target, cancel); }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    (snapshot.LogLines ??= new()).Add("SOURCE_ERROR " + source.Key + ": " + e.GetType().Name + " " + e.Message);
                }
            }
            return snapshot;
        }
    }
}
