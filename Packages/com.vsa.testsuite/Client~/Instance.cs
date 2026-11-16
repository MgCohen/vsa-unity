using System.Diagnostics;
using System.Text.Json;

namespace VSA.TestSuite.Client
{
    /// <summary>The address of one running Unity instance, read from its Pipeline descriptor file.</summary>
    public sealed class Instance
    {
        /// <summary>The descriptor file. Unity writes it again after each domain reload.</summary>
        public string DescriptorPath { get; }

        public int Port { get; private set; }
        public int Pid { get; private set; }
        internal string Token { get; private set; }

        Instance(string descriptorPath) => DescriptorPath = descriptorPath;

        /// <summary>An Editor: the descriptor is in the Library folder of the project.</summary>
        public static Instance ForEditor(string projectPath)
            => new Instance(Path.Combine(projectPath, "Library", "Pipeline", ".unity-pipeline-port"));

        /// <summary>A player: the descriptor is beside the executable. Start each player from its own folder.</summary>
        public static Instance ForPlayer(string playerFolder)
            => new Instance(Path.Combine(playerFolder, ".unity-pipeline-runtime-port"));

        /// <summary>Reads the descriptor. Returns false if the file is missing or not complete.</summary>
        public bool TryLoad()
        {
            try
            {
                if (!File.Exists(DescriptorPath)) return false;
                using var doc = JsonDocument.Parse(File.ReadAllText(DescriptorPath));
                var root = doc.RootElement;
                Port = root.GetProperty("port").GetInt32();
                Token = root.GetProperty("evalToken").GetString();
                Pid = root.TryGetProperty("pid", out var pid) ? pid.GetInt32() : 0;
                return Port > 0 && !string.IsNullOrEmpty(Token);
            }
            catch (Exception e) when (e is IOException || e is JsonException || e is KeyNotFoundException || e is InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>True if the process of the descriptor runs.</summary>
        public bool ProcessAlive()
        {
            if (Pid <= 0) return true;
            try
            {
                using var p = Process.GetProcessById(Pid);
                return !p.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public override string ToString() => "port " + Port + ", pid " + Pid;
    }
}
