using DiceGame.App.Views;
using DiceGame.Features.Counter.Presenters;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Factories;
using Umpire.Kernels;
using Umpire.Unity.Factories;

namespace DiceGame.App {
    // The registrations of the Unity views of the app. Features do not know the views, so they are here.
    public sealed class DiceViewsModule : Module {
        public override void Configure(IServiceCollection services) {
            // One controller for the app. The scene injection binds the RollListView of the scene to it.
            services.AddScoped<RollListController>();
            // A roll entry is made with its roll number. The container gives the presenter of the app scope.
            services.AddFactory<int, RollEntry>((provider, number) =>
                new RollEntry(number, provider.GetRequiredService<CounterPresenter>().Last));
            services.AddPrefabFactory<int, RollEntryView>();
        }
    }
}
