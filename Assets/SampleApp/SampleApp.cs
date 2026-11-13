using VSA.Composition;
using VSA.Unity;

namespace SampleApp {
    public sealed class SampleApp : VsaApp<SampleApp> {
        protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseUnityHost();
    }
}
