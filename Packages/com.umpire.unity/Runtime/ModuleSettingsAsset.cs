using System;
using Umpire.Kernels;
using UnityEngine;

namespace Umpire.Unity {
    // The settings of one module as a Unity asset. Put the assets on the shell (Settings).
    // Derive one class from ModuleSettingsAsset<TModule, TSettings> for each module.
    public abstract class ModuleSettingsAsset : ScriptableObject {
        // The module class that the settings are for.
        public abstract Type Module { get; }

        // Makes the settings object from the serialized fields of the asset.
        public abstract object Settings();
    }

    // Unity serializes fields, not properties, and it cannot serialize a Dictionary. So the derived class keeps
    // its data in Unity fields, and Create makes the settings object from them.
    public abstract class ModuleSettingsAsset<TModule, TSettings> : ModuleSettingsAsset
        where TModule : Module
        where TSettings : class, new() {
        public sealed override Type Module => typeof(TModule);

        public sealed override object Settings() => Create();

        protected abstract TSettings Create();
    }
}
