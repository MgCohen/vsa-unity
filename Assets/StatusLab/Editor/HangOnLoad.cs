using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace StatusLab
{
    // Blocks the main thread at each domain load while Temp/StatusLab/hang.flag exists.
    // Forces the states startup-hang and reload-hang. Only the escalation of the supervisor ends it.
    [InitializeOnLoad]
    static class HangOnLoad
    {
        static HangOnLoad()
        {
            if (!StatusLabGate.Active || !File.Exists(Path.Combine(StatusLabGate.Folder, "hang.flag"))) return;
            Debug.Log("STATUSLAB_HANG_START");
            Thread.Sleep(600_000);
            Debug.Log("STATUSLAB_HANG_END");
        }
    }
}
