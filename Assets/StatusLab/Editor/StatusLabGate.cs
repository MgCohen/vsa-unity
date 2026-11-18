using System.IO;

namespace StatusLab
{
    // The lab scripts run only in an Editor started with the beacon flag of the package (-vsaStatusBeacon).
    static class StatusLabGate
    {
        public static readonly bool Active = VSA.TestSuite.Editor.StatusBeacon.Enabled;

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
