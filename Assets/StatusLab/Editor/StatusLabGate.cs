using System;
using System.IO;

namespace StatusLab
{
    // The lab scripts run only in an Editor that the spike started with -statuslab.
    static class StatusLabGate
    {
        public static readonly bool Active = Array.IndexOf(Environment.GetCommandLineArgs(), "-statuslab") >= 0;

        // Library/StatusLab/ of this project: the beacon file and hang.flag.
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
