using Greeter;
using Umpire.Unity;

namespace VContainerSample {
    public sealed class GreeterHost : UnityHost {
        public GreeterHost() : base(new GreeterModule()) {
        }
    }
}
