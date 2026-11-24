using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record HoldObject(string Path, float Seconds) : TestRequest<HoldObjectResult>;
}
