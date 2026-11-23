using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;

namespace Spike {
    // D1 to D5: open handlers that dispatch closes. Coin has no [AutoClose], and no constructor asks for these types.
    // D1 and D2 use PutHandler<T>, StoreHandler<T> and RangeValidator<T> from SpikeTypes.cs. D5 uses LogChanged<T>.

    // D3: a declared message class of the shape. The scan can close RelayHandler<Coin> at startup.
    public record Relay<T>(T Value) : ICommand<string> where T : struct;

    public sealed record CoinRelay(Coin Value) : Relay<Coin>(Value);

    public sealed class RelayHandler<T> : CommandHandler<Relay<T>, string> where T : struct {
        public override Task<Result<string>> HandleAsync(IEnvelope<Relay<T>> envelope) =>
            Task.FromResult<Result<string>>("relay " + typeof(T).Name);
    }

    // D4: a handler with two type parameters, closed at dispatch.
    public sealed record Swap<TFrom, TTo>(int Amount) : ICommand<string> where TFrom : struct where TTo : struct;

    public sealed class SwapHandler<TFrom, TTo> : CommandHandler<Swap<TFrom, TTo>, string> where TFrom : struct where TTo : struct {
        public override Task<Result<string>> HandleAsync(IEnvelope<Swap<TFrom, TTo>> envelope) =>
            Task.FromResult<Result<string>>(typeof(TFrom).Name + "->" + typeof(TTo).Name);
    }
}
