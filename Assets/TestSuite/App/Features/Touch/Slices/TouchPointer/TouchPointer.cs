using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Features.Touch
{
    public sealed record TouchPointer(Point Point) : ICommand<PointerHit>;
}
