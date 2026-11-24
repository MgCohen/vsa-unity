using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record HoldObjectResult(string Path, string Name, Point Point, string Hit, int Frames, float Seconds)
        : IResponse;
}
