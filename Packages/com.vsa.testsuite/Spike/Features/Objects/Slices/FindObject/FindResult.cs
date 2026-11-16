using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record FindResult(string Path, string Name, Point Point) : IResponse;
}
