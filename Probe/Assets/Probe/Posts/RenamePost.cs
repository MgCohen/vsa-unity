using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;

namespace Probe.Posts {
    public sealed record RenamePostCommand(int Id, string Title) : ICommand<RenamePostResult>;

    public sealed record RenamePostResult(string Title);

    sealed class RenamePostHandler : CommandHandler<RenamePostCommand, RenamePostResult> {
        readonly PostStore store;

        public RenamePostHandler(PostStore store) {
            this.store = store;
        }

        public override Task<Result<RenamePostResult>> HandleAsync(Envelope<RenamePostCommand> envelope) {
            var index = envelope.Message.Id - 1;
            store.Posts[index] = store.Posts[index] with { Title = envelope.Message.Title };
            return Task.FromResult<Result<RenamePostResult>>(new RenamePostResult(store.Posts[index].Title));
        }
    }
}
