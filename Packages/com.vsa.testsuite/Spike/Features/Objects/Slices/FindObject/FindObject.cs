using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record FindObject(string Path) : IRequest<FindResult>;
}
