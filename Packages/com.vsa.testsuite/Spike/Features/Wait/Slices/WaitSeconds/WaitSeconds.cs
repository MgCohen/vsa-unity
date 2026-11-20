using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Wait
{
    public sealed record WaitSeconds(float Seconds) : TestRequest, IRequest<WaitResult>;
}
