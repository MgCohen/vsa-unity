using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Wait
{
    public sealed record WaitSeconds(float Seconds) : TestRequest<WaitResult>;
}
