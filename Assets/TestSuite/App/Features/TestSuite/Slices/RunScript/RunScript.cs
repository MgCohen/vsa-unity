using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.TestSuite
{
    public sealed record RunScript(string Name) : TestRequest<ScriptResult>, IScriptRun;
}
