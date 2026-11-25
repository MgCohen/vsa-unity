using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record AssertText(string Path, string Text) : TestRequest<TextResult>;
}
