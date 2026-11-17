using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    public sealed record RunScript(string Name) : IRequest<ScriptResult>;
}
