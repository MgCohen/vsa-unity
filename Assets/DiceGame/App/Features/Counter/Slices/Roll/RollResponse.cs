using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.Roll {
    public sealed record RollResponse(int Value, int Count) : IResponse;
}
