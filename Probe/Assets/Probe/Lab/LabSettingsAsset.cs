using System;
using System.Collections.Generic;
using System.Linq;
using Umpire.Unity;
using UnityEngine;

namespace Probe.Lab {
    // The LabModule settings as an asset. Unity cannot serialize a Dictionary, so Weights is a list of pairs.
    [CreateAssetMenu(menuName = "Probe/Lab Settings")]
    public sealed class LabSettingsAsset : ModuleSettingsAsset<LabModule, LabSettings> {
        [Serializable]
        public struct Weight {
            public string Key;
            public int Value;
        }

        public string Greeting = "default";
        public int Limit = 1;
        public List<string> Tags = new List<string>();
        public List<Weight> Weights = new List<Weight>();
        public LabMode Mode;

        protected override LabSettings Create() => new LabSettings {
            Greeting = Greeting,
            Limit = Limit,
            Tags = new List<string>(Tags),
            Weights = Weights.ToDictionary(w => w.Key, w => w.Value),
            Mode = Mode,
        };

#if UNITY_EDITOR
        public const string Path = "Assets/Probe/Lab/LabSettings.asset";

        // Writes the probe asset with fixed values, and returns it. The probe scene puts it on the shell.
        public static LabSettingsAsset Write() {
            UnityEditor.AssetDatabase.DeleteAsset(Path);
            var asset = CreateInstance<LabSettingsAsset>();
            asset.Greeting = "asset";
            asset.Limit = 9;
            asset.Tags = new List<string> { "b" };
            asset.Weights = new List<Weight> { new Weight { Key = "y", Value = 3 } };
            asset.Mode = LabMode.On;
            UnityEditor.AssetDatabase.CreateAsset(asset, Path);
            UnityEditor.AssetDatabase.SaveAssets();
            return asset;
        }
#endif
    }
}
