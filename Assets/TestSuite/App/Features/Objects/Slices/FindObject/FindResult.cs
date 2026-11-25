using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Features.Touch;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record FindResult(string Path, string Name, Point Point) : IResponse;
}
