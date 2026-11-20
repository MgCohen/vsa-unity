using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record TouchObject(string Path) : TestRequest, IRequest<TouchObjectResult>;
}
