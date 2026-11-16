
namespace VSA.TestSuite.Spike
{
    internal sealed record TouchScreen(Point Point) : IAction<TouchResult>;
}
