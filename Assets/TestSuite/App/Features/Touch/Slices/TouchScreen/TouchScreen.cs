using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record TouchScreen(Point Point) : TestRequest<TouchResult>;
}
