using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record OverlayResult(bool On) : IResponse;
}
