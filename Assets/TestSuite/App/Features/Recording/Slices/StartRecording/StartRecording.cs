using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Recording
{
    public sealed record StartRecording(string Name) : TestRequest<StartResult>;
}
