using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Wait
{
    public sealed record WaitFrames(int Count) : TestRequest<WaitResult>;
}
