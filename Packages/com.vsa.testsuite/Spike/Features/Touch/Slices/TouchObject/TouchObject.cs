using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record TouchObject(string Path) : IRequest<TouchObjectResult>, IAction;
}
