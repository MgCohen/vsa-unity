using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Scenes
{
    public sealed record LoadScene(string Name, bool Additive) : TestRequest<SceneResult>;
}
