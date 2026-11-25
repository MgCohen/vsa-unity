using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record HoldResult(Point Point, string Hit, int Frames, float Seconds) : IResponse;
}
