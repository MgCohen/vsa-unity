using DiceGame.Features.Counter.Domain;
using DiceGame.Features.Counter.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Infrastructure;

namespace DiceGame.Features.Counter {
    // The counter feature: it rolls the die of the dice plugin and counts the rolls.
    public sealed class CounterModule : Module {
        public override void Configure(IServiceCollection services) {
            services.AddSingleton<RollLogStore>();
            services.AddScoped<IRepository<RollLog, int>, RollLogRepository>();
        }
    }
}
