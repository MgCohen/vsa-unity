using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record HoldPointer(Point Point, float Seconds) : ICommand<PointerHold>;
}
