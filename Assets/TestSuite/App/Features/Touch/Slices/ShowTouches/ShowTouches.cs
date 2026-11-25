using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record ShowTouches(bool On) : TestRequest<OverlayResult>;
}
