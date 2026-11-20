using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record DragScreen(Point From, Point To, int Steps) : TestRequest, IRequest<DragResult>;
}
