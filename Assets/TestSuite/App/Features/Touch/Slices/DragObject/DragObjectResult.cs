using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record DragObjectResult(string Path, string To, Point From, Point ToPoint, string Hit, int Frames)
        : IResponse;
}
