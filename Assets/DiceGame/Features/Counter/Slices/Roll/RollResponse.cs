using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.Roll {
    // The value of the roll and the number of rolls after it.
    public sealed record RollResponse(int Value, int Count) : IResponse;
}
