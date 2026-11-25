using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record TextResult(string Path, string Text) : IResponse;
}
