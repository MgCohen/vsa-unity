using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.Roll {
    // Rolls one six-sided die and counts the roll.
    public sealed record RollRequest : IRequest<RollResponse>;
}
