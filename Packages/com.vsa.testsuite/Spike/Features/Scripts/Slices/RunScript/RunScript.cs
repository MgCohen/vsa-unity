using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    public sealed record RunScript(string Name) : TestRequest, IRequest<ScriptResult>, IScriptRun;
}
