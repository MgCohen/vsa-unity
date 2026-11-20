using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

namespace Probe.Posts {
    public sealed record AddPostRequest(string Title) : IRequest<AddPostResponse>;

    public sealed record AddPostResponse(int Id) : IResponse;

    sealed class AddPostHandler : RequestHandler<AddPostRequest, AddPostResponse> {
        readonly PostStore store;
        readonly IIntegrationEventPublisher events;

        public AddPostHandler(PostStore store, IIntegrationEventPublisher events) {
            this.store = store;
            this.events = events;
        }

        public override async Task<Result<AddPostResponse>> HandleAsync(IEnvelope<AddPostRequest> envelope) {
            var post = new Post(store.Posts.Count + 1, envelope.Message.Title);
            store.Posts.Add(post);
            await events.PublishAsync(new PostAdded(post.Id), envelope.Cancellation);
            return new AddPostResponse(post.Id);
        }
    }
}
