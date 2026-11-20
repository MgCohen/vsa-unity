using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed record StartResult(string Recorder, string File) : IResponse;
}
