using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace Probe.Lab {
    // A module with settings. The settings come from the ISettingsProvider list of the host.
    public sealed class LabModule : Module<LabSettings> {
        public static LabSettings Bound { get; private set; }

        public override void Configure(IServiceCollection services, LabSettings settings) {
            Bound = settings;
        }
    }

    public sealed class LabSettings {
        public string Greeting { get; set; } = "default";
        public int Limit { get; set; } = 1;
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, int> Weights { get; set; }
        public LabMode Mode { get; set; }
    }

    public enum LabMode {
        Off,
        On,
    }
}
