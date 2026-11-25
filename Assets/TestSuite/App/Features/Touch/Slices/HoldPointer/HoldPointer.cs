using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record HoldPointer(Point Point, float Seconds) : ICommand<PointerHold>;
}
