using System.Threading.Tasks;
using DiceGame.Features.Counter.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

namespace DiceGame.Features.Counter.Slices.GetCount {
    // Reads the roll log.
    sealed class GetCountHandler : RequestHandler<GetCountRequest, GetCountResponse> {
        readonly IRepository<RollLog, int> logs;

        public GetCountHandler(IRepository<RollLog, int> logs) => this.logs = logs;

        public override async Task<Result<GetCountResponse>> HandleAsync(Envelope<GetCountRequest> envelope) {
            var log = await logs.GetAsync(RollLog.OnlyId, envelope.Cancellation);
            return log == null ? new GetCountResponse(0, 0) : new GetCountResponse(log.Count, log.Last);
        }
    }
}
