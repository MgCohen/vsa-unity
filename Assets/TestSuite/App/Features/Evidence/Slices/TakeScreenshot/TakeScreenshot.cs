using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Evidence
{
    public sealed record TakeScreenshot(string Name) : TestRequest<ScreenshotResult>;
}
