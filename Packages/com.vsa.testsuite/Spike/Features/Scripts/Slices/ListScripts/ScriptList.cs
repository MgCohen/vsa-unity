using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    public sealed record ScriptList(string[] Names) : IResponse;
}
