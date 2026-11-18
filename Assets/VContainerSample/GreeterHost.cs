using Greeter;
using Umpire.Unity;

namespace VContainerSample {
    // The Unity host of the sample with its module list. The module scan of Umpire would also find the Sample
    // module, because the editor loads each assembly of the project.
    public sealed class GreeterHost : UnityHost {
        public GreeterHost() : base(new GreeterModule()) {
        }
    }
}
