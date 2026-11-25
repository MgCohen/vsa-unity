using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record TouchObjectResult(string Path, string Name, Point Point, string Hit) : IResponse;
}
