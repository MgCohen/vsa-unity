using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Evidence
{
    public sealed record ScreenshotResult(string Path) : IResponse;
}
