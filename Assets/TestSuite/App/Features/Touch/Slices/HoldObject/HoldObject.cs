using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record HoldObject(string Path, float Seconds) : TestRequest<HoldObjectResult>;
}
