using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public sealed record ResetScene(string Name, int? Seed) : TestRequest<SceneResult>;
}
