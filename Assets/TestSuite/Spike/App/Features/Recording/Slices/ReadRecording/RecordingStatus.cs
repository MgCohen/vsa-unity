using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed record RecordingStatus(string Recorder, string Unavailable, bool Active, string File) : IResponse;
}
