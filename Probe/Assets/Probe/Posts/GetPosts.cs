using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

namespace Probe.Posts {
    public sealed record GetPostsRequest : IRequest<GetPostsResponse>;

    public sealed record GetPostsResponse(int Count) : IResponse;

    sealed class GetPostsHandler : RequestHandler<GetPostsRequest, GetPostsResponse> {
        readonly PostStore store;

        public GetPostsHandler(PostStore store) {
            this.store = store;
        }

        public override Task<Result<GetPostsResponse>> HandleAsync(Envelope<GetPostsRequest> envelope) =>
            Task.FromResult<Result<GetPostsResponse>>(new GetPostsResponse(store.Posts.Count));
    }
}
