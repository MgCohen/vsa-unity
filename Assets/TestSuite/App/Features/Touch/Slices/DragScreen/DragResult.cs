using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record DragResult(Point From, Point To, int Steps, string Hit, int Frames) : IResponse;
}
