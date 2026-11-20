using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public sealed record SceneResult(string Scene, bool Additive, int StartFrame, int Frames, float Seconds) : IResponse;
}
