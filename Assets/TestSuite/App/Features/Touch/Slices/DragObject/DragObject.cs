using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record DragObject(string Path, string To, int Steps) : TestRequest<DragObjectResult>;
}
