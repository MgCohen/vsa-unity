using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record OverlayResult(bool On) : IResponse;
}
