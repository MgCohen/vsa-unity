using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.TestSuite
{
    public sealed record ScriptList(string[] Names) : IResponse;
}
