using Sample;
using Umpire.Unity;

namespace SampleApp {
    public sealed class SampleHost : UnityHost {
        public SampleHost() : base(new SampleModule()) {
        }
    }
}
