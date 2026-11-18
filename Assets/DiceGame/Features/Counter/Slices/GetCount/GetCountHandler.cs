using System.Threading.Tasks;
using DiceGame.Features.Counter.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace DiceGame.Features.Counter.Slices.GetCount {
    // Reads the roll log.
    sealed class GetCountHandler : RequestHandler<GetCountRequest, GetCountResponse> {
        readonly RollLog log;

        public GetCountHandler(RollLog log) => this.log = log;

        public override Task<Result<GetCountResponse>> HandleAsync(Envelope<GetCountRequest> envelope) =>
            Task.FromResult<Result<GetCountResponse>>(new GetCountResponse(log.Count, log.Last));
    }
}
