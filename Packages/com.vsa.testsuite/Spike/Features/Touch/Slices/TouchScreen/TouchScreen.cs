using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record TouchScreen(Point Point) : IRequest<TouchResult>, IAction;
}
