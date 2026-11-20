using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record WaitForObject(string Path, ObjectState State, float Seconds)
        : TestRequest<WaitForResult>;
}
