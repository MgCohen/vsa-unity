using Sample;
using Umpire.Unity;

namespace SampleApp {
    // The Unity host of the sample with its module list. The module scan of Umpire would also find the Greeter
    // module of the VContainer sample, because the editor loads each assembly of the project.
    public sealed class SampleHost : UnityHost {
        public SampleHost() : base(new SampleModule()) {
        }
    }
}
