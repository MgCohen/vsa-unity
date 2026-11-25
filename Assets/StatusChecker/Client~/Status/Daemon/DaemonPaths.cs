using System.Security.Cryptography;
using System.Text;

namespace VSA.StatusChecker
{
    /// <summary>The names of the daemon of one project: its pipe, its lock file and its log.</summary>
    public static class DaemonPaths
    {
        /// <summary>The pipe name. The same project path always gives the same name.</summary>
        public static string PipeName(string projectPath)
        {
            var key = Path.GetFullPath(projectPath).TrimEnd('\\', '/').ToLowerInvariant();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
            return "vsa-unity-" + hash.Substring(0, 16).ToLowerInvariant();
        }

        /// <summary>The lock file. The daemon holds it open with no sharing, so a second daemon cannot start.</summary>
        public static string Lock(Target target) => Path.Combine(target.StatusFolder, "daemon.lock");

        /// <summary>The text log of the daemon process.</summary>
        public static string Log(Target target) => Path.Combine(target.StatusFolder, "daemon.log");
    }
}
