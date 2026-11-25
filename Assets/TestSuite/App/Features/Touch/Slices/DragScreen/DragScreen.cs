using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record DragScreen(Point From, Point To, int Steps) : TestRequest<DragResult>;
}
