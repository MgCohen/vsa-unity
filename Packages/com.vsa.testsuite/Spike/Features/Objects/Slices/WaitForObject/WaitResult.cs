using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record WaitResult(string Path, string Name, int Frames, float Seconds) : IResponse;
}
