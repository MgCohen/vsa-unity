using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.Roll {
    public sealed record RollRequest : IRequest<RollResponse>;
}
