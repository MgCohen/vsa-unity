using System.Diagnostics;

namespace StatusLab
{
    /// <summary>The own Editor: project, PID, log file and server address.</summary>
    public sealed class Target
    {
        public string ProjectPath;
        public string LogPath;
        public string EditorId;
        public DateTime StartedUtc;
        public int Pid;
        public Process Process;
        public string ExtraArgs;          // more Editor arguments, for example "-ignoreCompilerErrors"           // opened at once after the start, so the exit code stays readable

        // Read from the descriptor by DescriptorSource. The token never leaves memory.
        internal int Port;
        internal string Token;

        public string DescriptorPath => Path.Combine(ProjectPath, "Library", "Pipeline", ".unity-pipeline-port");
        public string LabFolder => Path.Combine(ProjectPath, "Library", "StatusLab");
    }
}
