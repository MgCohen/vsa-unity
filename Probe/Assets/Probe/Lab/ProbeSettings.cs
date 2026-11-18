using System.Collections.Generic;
using Umpire.Kernels;
using Umpire.Unity;

namespace Probe.Lab {
    // The settings of the probe modules, written in code. No reflection reads them.
    public sealed class ProbeSettings : UnitySettings {
        public override TSettings Get<TSettings>(Module module) {
            if (module is LabModule) {
                return new LabSettings {
                    Greeting = "hello",
                    Limit = 7,
                    Tags = new List<string> { "a" },
                    Weights = new Dictionary<string, int> { ["x"] = 2 },
                    Mode = LabMode.On,
                } as TSettings;
            }
            return null;
        }
    }
}
