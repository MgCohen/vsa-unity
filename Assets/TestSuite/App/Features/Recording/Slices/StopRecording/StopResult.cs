using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Recording
{
    public sealed record StopResult(string File, float Seconds, string Reason, long Bytes) : IResponse;
}
