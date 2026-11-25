using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record TouchObject(string Path) : TestRequest<TouchObjectResult>;
}
