using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record ScriptList(string[] Names) : IResponse;
}
