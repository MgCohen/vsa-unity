using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record DragObject(string Path, string To, int Steps) : TestRequest<DragObjectResult>;
}
