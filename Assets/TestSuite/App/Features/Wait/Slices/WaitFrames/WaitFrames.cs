using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Wait
{
    public sealed record WaitFrames(int Count) : TestRequest<WaitResult>;
}
