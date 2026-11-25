using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Scenes
{
    public sealed record ResetScene(string Name, int? Seed) : TestRequest<SceneResult>;
}
