using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed record StartRecording(string Name) : TestRequest, IRequest<StartResult>;
}
