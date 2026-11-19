using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.GetCount {
    // The number of rolls and the last value.
    public sealed record GetCountResponse(int Count, int Last) : IResponse;
}
