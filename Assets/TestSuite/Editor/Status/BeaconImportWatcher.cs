using UnityEditor;

namespace VSA.TestSuite.Editor
{
    /// <summary>Gives the status beacon an "import" event after each asset import.</summary>
    sealed class BeaconImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Length + deleted.Length + moved.Length > 0) StatusBeacon.Event("import");
        }
    }
}
