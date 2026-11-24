using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record DragPointer(Point From, Point To, int Steps) : ICommand<PointerDrag>;
}
