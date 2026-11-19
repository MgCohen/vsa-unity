using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record WaitForObject(string Path, float Seconds) : TestRequest, IRequest<WaitResult>;
}
