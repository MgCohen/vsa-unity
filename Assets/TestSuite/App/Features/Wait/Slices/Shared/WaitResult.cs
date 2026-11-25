using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Wait
{
    public sealed record WaitResult(int Frames, float Seconds) : IResponse;
}
