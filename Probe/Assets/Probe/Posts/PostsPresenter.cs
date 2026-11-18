using System.Threading;
using System.Threading.Tasks;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

namespace Probe.Posts {
    public sealed class PostsPresenter : Presenter, IPresenterEvent<PostAdded> {
        readonly TaskCompletionSource<int> eventThread =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        public PostsPresenter(IRequestSender requests) : base(requests) {
        }

        public int Count { get; private set; }
        public OpState Load { get; } = new OpState();

        // The managed thread id of the first PostAdded event.
        public Task<int> EventThread => eventThread.Task;

        public Task LoadAsync() => Run(Load, async () => {
            var posts = await Requests.SendAsync(new GetPostsRequest());
            if (posts.IsSuccess) {
                Count = posts.Value.Count;
            }
            return posts.Error;
        });

        public Task OnEventAsync(PostAdded integrationEvent, CancellationToken ct) {
            Count++;
            eventThread.TrySetResult(Thread.CurrentThread.ManagedThreadId);
            return Task.CompletedTask;
        }
    }
}
