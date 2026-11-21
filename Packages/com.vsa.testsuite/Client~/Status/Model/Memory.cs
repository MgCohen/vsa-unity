namespace VSA.TestSuite.Client.Status
{
    /// <summary>What the status tracker keeps between samples. Only StatusTracker writes it.</summary>
    public sealed class Memory
    {
        public int OwnPid;
        public string LastState;
        public long StateSinceMs;
        public long LastProgressMs;
        public bool EverReady;
        public bool FullImport;
        public int Recoveries;
        /// <summary>The supervisor or the playbook stopped the own Editor: its exit is not a start failure.</summary>
        public bool StopSent;
        /// <summary>The client attached to an instance that another tool started: the start flags are not ours to check.</summary>
        public bool Attached;

        // Fingerprint of the last progress signals.
        public long LogSize;
        public string ProgressText;
        public long BeaconSeq;
        public double CpuMs;
        public int? BusyBar;
        public string BusyStep;
        public int? SplashBar;

        // From the action log.
        public long? LastPlayCommandMs;
        public bool JobActive;
        public readonly Dictionary<string, int> StepFailures = new();
        public readonly Dictionary<string, int> DialogCounts = new();

        /// <summary>Forget the own Editor after its exit is recorded.</summary>
        public void ForgetEditor()
        {
            OwnPid = 0;
            EverReady = false;
            FullImport = false;
            DialogCounts.Clear();
            StopSent = false;
        }
    }
}
