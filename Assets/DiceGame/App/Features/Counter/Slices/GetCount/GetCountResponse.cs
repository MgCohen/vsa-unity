using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.GetCount {
    public sealed record GetCountResponse(int Count, int Last) : IResponse;
}
