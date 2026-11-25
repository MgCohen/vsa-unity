using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record HoldScreen(Point Point, float Seconds) : TestRequest<HoldResult>;
}
