using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record WaitForObject(string Path, ObjectState State, float Seconds)
        : TestRequest<WaitForResult>;
}
