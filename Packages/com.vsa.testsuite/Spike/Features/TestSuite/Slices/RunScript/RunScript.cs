using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.TestSuite
{
    public sealed record RunScript(string Name) : TestRequest, IRequest<ScriptResult>, IScriptRun;
}
