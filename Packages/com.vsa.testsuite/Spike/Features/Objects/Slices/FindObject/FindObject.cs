using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record FindObject(string Path) : TestRequest<FindResult>;
}
