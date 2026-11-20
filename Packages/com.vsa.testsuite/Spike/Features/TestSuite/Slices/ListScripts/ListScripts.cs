using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.TestSuite
{
    public sealed record ListScripts : TestRequest, IRequest<ScriptList>;
}
