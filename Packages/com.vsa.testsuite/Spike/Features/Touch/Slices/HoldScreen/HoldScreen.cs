using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record HoldScreen(Point Point, float Seconds) : TestRequest<HoldResult>;
}
