using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record AssertState(string Path, ObjectState State) : TestRequest<StateResult>;
}
