using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed record TouchPointer(Point Point) : ICommand<PointerHit>;
}
