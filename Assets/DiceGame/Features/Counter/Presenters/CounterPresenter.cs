using System.Threading.Tasks;
using DiceGame.Features.Counter.Slices.GetCount;
using DiceGame.Features.Counter.Slices.Roll;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;

namespace DiceGame.Features.Counter.Presenters {
    // The state of the counter view: the number of rolls and the last value.
    public sealed class CounterPresenter : Presenter {
        public CounterPresenter(IRequestSender requests) : base(requests) {
        }

        public int Count { get; private set; }

        public int Last { get; private set; }

        public OpState Load { get; } = new OpState();

        public OpState Roll { get; } = new OpState();

        public Task LoadAsync() => Run(Load, async () => {
            var result = await Requests.SendAsync(new GetCountRequest());
            if (result.IsSuccess) {
                Count = result.Value.Count;
                Last = result.Value.Last;
            }
            return result.Error;
        });

        public Task RollAsync() => Run(Roll, async () => {
            var result = await Requests.SendAsync(new RollRequest());
            if (result.IsSuccess) {
                Count = result.Value.Count;
                Last = result.Value.Value;
            }
            return result.Error;
        });
    }
}
