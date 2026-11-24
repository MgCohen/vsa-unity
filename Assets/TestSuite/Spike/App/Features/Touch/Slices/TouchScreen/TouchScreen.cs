using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record TouchScreen(Point Point) : TestRequest<TouchResult>;
}
