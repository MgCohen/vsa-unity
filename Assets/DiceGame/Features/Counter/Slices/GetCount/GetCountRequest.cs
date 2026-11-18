using Umpire.Vsa.Contracts.Requests;

namespace DiceGame.Features.Counter.Slices.GetCount {
    // Gives the number of rolls and the last value.
    public sealed record GetCountRequest : IRequest<GetCountResponse>;
}
