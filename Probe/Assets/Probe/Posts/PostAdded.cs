using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

namespace Probe.Posts {
    public sealed record PostAdded(int Id) : IntegrationEvent;

    sealed class CountPublished : IntegrationEventHandler<PostAdded> {
        readonly PostStore store;

        public CountPublished(PostStore store) {
            this.store = store;
        }

        public override Task<Result<Unit>> HandleAsync(Envelope<PostAdded> envelope) {
            store.Published++;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }
}
