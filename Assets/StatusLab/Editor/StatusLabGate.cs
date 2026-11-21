using System.IO;

namespace StatusLab
{
    // The lab scripts run only in an Editor started with -automated, as the status tools start it.
    static class StatusLabGate
    {
        public static readonly bool Active = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-automated") >= 0;

        // Library/StatusLab/ of this project: hang.flag.
        // Not Temp/, because Unity can clear Temp/ at a start.
        public static string Folder
        {
            get
            {
                var path = Path.GetFullPath("Library/StatusLab");
                Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}
