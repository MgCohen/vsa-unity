using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record TouchResult(Point Point, string Hit) : IResponse;
}
