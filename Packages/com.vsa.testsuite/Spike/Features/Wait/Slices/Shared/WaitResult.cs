using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Wait
{
    public sealed record WaitResult(int Frames, float Seconds) : IResponse;
}
