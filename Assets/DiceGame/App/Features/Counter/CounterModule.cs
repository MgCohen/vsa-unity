using DiceGame.Features.Counter.Domain;
using DiceGame.Features.Counter.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Infrastructure;

namespace DiceGame.Features.Counter {
    public sealed class CounterModule : Module {
        protected override void Configure(IServiceCollection services) {
            services.AddSingleton<RollLogStore>();
            services.AddScoped<IRepository<RollLog, int>, RollLogRepository>();
        }
    }
}
