using Umpire.Kernels;

namespace Umpire.Unity {
    // Gives the settings of the ModuleSettingsAsset assets on the shell that owns the app.
    sealed class AssetSettings : ISettingsProvider {
        public TSettings Get<TSettings>(Module module) where TSettings : class, new() {
            var owner = Shell.Owner;
            if (owner == null) {
                return null;
            }
            foreach (var asset in owner.SettingsAssets) {
                if (asset != null && asset.Module == module.GetType()) {
                    return asset.Settings() as TSettings;
                }
            }
            return null;
        }
    }
}
