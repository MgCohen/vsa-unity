using System.Diagnostics;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>The own Editor: project, PID, log file and server address.</summary>
    public sealed class Target
    {
        /// <summary>The Editor command-line flag that starts the status beacon of the package.</summary>
        public const string BeaconFlag = "-vsaStatusBeacon";

        /// <summary>The Pipeline command of the main-thread probe in an Editor (Editor assembly of the package).</summary>
        public static readonly string[] EditorProbe = { "suite", "ping" };

        /// <summary>The Pipeline command of the main-thread probe in a player.</summary>
        public static readonly string[] PlayerProbe = { "game.info" };

        public string ProjectPath;
        public string LogPath;
        public string EditorId;
        public DateTime StartedUtc;
        public int Pid;
        /// <summary>Opened at once after the start, so the exit code stays readable.</summary>
        public Process Process;
        /// <summary>More Editor arguments, for example "-ignoreCompilerErrors".</summary>
        public string ExtraArgs;
        /// <summary>The root folder of the run folders. Null: Logs/vsa-unity in the project.</summary>
        public string LogRoot;
        /// <summary>The descriptor file. Null: Library/Pipeline/.unity-pipeline-port in the project.</summary>
        public string DescriptorFile;
        /// <summary>The probe command. Null: EditorProbe.</summary>
        public string[] Probe;

        /// <summary>Read from the descriptor by DescriptorSource. The token never leaves memory.</summary>
        internal int Port;
        internal string Token;

        public string DescriptorPath => DescriptorFile ?? Path.Combine(ProjectPath, "Library", "Pipeline", ".unity-pipeline-port");

        /// <summary>Library/VSA.TestSuite/Status: the beacon file, own-editor.json and watch.json.</summary>
        public string StatusFolder => ProjectPath == null ? null : Path.Combine(ProjectPath, "Library", "VSA.TestSuite", "Status");

        /// <summary>The folder of the run folders, one for each Editor start.</summary>
        public string RunRoot => LogRoot ?? (ProjectPath == null ? null : Path.Combine(ProjectPath, "Logs", "vsa-unity"));

        /// <summary>The folder of this Editor run: unity.log and watch.ndjson.</summary>
        public string RunFolder => EditorId == null ? null : Path.Combine(RunRoot, EditorId);

        /// <summary>The Pipeline port of the own Editor, from the last descriptor read. 0: none.</summary>
        public int ServerPort => Port;
    }
}
