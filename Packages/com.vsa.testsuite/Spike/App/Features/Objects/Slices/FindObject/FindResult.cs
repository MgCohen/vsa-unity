using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Features.Touch;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record FindResult(string Path, string Name, Point Point) : IResponse;
}
