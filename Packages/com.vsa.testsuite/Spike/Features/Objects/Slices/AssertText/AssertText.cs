using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record AssertText(string Path, string Text) : TestRequest, IRequest<TextResult>;
}
