using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.TestSuite
{
    public sealed record ScriptList(string[] Names) : IResponse;
}
