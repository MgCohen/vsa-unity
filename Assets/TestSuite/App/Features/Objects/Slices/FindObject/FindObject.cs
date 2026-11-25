using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record FindObject(string Path) : TestRequest<FindResult>;
}
