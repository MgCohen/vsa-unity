using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    public sealed record TakeScreenshot(string Name) : TestRequest, IRequest<ScreenshotResult>;
}
