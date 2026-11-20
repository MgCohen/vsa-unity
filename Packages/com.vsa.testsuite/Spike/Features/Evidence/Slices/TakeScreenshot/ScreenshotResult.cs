using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    public sealed record ScreenshotResult(string Path) : IResponse;
}
