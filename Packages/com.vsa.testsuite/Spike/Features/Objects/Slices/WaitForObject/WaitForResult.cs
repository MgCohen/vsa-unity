using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record WaitForResult(string Path, ObjectState State, int Frames, float Seconds) : IResponse;
}
