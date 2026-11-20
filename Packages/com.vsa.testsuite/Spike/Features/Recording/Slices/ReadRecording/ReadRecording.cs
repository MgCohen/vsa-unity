using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Recording
{
    public sealed record ReadRecording : TestRequest, IRequest<RecordingStatus>;
}
