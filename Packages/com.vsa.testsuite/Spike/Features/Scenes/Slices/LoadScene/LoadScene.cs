using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public sealed record LoadScene(string Name, bool Additive) : TestRequest, IRequest<SceneResult>;
}
