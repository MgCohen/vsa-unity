namespace VSA.TestSuite.Client.Status
{
    /// <summary>One sample of all signals. Fields are null or 0 when a source could not read them.</summary>
    public sealed class Snapshot
    {
        public long T;                    // ms on the clock of the recording

        // status: GET /api/status (0 = no answer)
        public int StatusHttp;
        public string Status;
        public bool? PlayerLoopTicking;

        // progress: GET /api/progress
        public bool? ProgressActive;
        public string ProgressText;

        // u-status and editors: CLI, refreshed about every 6 s
        public string UnityStatus;
        public string EditorsPhase;
        public long CliAgeMs;

        // probe: POST /api/exec suite ping, timeout 2000 ms (0 = no answer)
        public int ProbeHttp;
        public double ProbeMs;
        public string ProbeBusyReason;
        public bool ProbeOk;
        /// <summary>The probe used GET /api/editor_status, because the Editor has no "suite ping".</summary>
        public bool ProbeFallback;
        public int? ProbeFrame;
        public bool? ProbeCompiling;
        public bool? ProbeUpdating;
        public bool? ProbePlaying;
        public bool? ProbePaused;
        public int? ProbeDirtyScenes;
        /// <summary>The paths of the dirty scenes, separated by "|". An unsaved new scene has the name "(untitled)".</summary>
        public string ProbeDirtyScenePaths;

        // truth: console status groundTruth
        public bool? TruthCompiling;
        public bool? TruthFailed;
        public int? TruthErrors;

        // desc: Library/Pipeline/.unity-pipeline-port (the token is never stored)
        public bool DescPresent;
        public int DescPid;
        public bool DescPidAlive;
        public double? DescHeartbeatAgeS;
        public bool DescInfoSet;

        // proc: the own PID
        public bool ProcAlive;
        public double ProcCpuPct;
        public double ProcCpuMs;
        public int? ProcExitCode;

        // win: Win32 windows of the own PID
        public string DialogTitle;
        public string DialogButtons;
        public string MainTitle;
        public bool? Hung;
        public string BusyWindow;         // Unity busy window: "Reloading Domain", "Importing", ...
        public int? BusySeconds;          // from "(busy for Ns)" in its title
        public int? BusyBar;              // busy window progress control, 0-1000
        public string BusyStep;           // busy window step text, for example the asset being imported
        public int? SplashBar;            // startup window (UnitySplashWindow) progress control, 0-100

        // log: unity.log of the own Editor
        public long LogSize;
        public List<string> LogLines;     // new marker lines since the last sample
        public int? LogIsHuman;
        public int? LogStartBugReporter;
        public bool LogExitSeen;
        public bool LogCrashSeen;         // "Crash!!!": the process can stay alive about 30 s after it

        // crash: Unity crash folder
        public bool CrashNew;

        // beacon: beacon.json in the status folder, own PID only
        public bool BeaconOwn;
        public long BeaconSeq;
        public double? BeaconAgeS;
        public bool? BeaconCompiling;
        public bool? BeaconUpdating;
        public bool? BeaconPlaying;
        public bool? BeaconPaused;
        public int? BeaconDirtyScenes;
        public string BeaconLastEvent;
        public double? BeaconLastEventAgeS;

        /// <summary>The server answers, and it is the server of the own Editor.</summary>
        public bool ServerUp(Memory memory) => StatusHttp == 200 && DescPresent && DescPid == memory.OwnPid;

        /// <summary>The beacon file belongs to the own Editor and was written in the last 3 s.</summary>
        public bool BeaconFresh => BeaconOwn && BeaconAgeS < 3;
    }
}
