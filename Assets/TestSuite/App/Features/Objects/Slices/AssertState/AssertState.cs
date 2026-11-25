using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record AssertState(string Path, ObjectState State) : TestRequest<StateResult>;
}
