using System.Threading;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace StatusLab
{
    // Imports a ".slowimport" file in 5 s, so a 500 ms sampler always sees the import (step A2).
    [ScriptedImporter(1, "slowimport")]
    class SlowImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Thread.Sleep(5000);
            var text = new TextAsset("slow import");
            ctx.AddObjectToAsset("main", text);
            ctx.SetMainObject(text);
        }
    }
}
