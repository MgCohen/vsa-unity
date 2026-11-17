using UnityEditor;

namespace StatusLab
{
    // Gives the beacon an "import" event after each asset import.
    class BeaconImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Length + deleted.Length + moved.Length > 0) StatusBeacon.Event("import");
        }
    }
}
