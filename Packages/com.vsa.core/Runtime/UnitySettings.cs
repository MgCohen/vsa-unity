using Umpire.Kernels;
using Umpire.Markers;

namespace Umpire.Unity {
    // A source of module settings in code. The Unity host finds each concrete derived class in the app and the
    // modules, and asks them in name order, after the assets on the shell. The first source that gives settings wins.
    public abstract class UnitySettings : ISettingsProvider {
        // Returns the settings of the module, or null if this source has none.
        public abstract TSettings Get<TSettings>(Module module) where TSettings : class, new();
    }
}
