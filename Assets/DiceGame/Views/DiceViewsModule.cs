using DiceGame.Features.Counter.Presenters;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Factories;
using Umpire.Kernels;
using Umpire.Unity.Factories;

namespace DiceGame.App.Views {
    // The registrations of the Unity views of the app. Features do not know the views, so they are here.
    // RollListController needs no registration: the host registers the target of each scene view as scoped.
    public sealed class DiceViewsModule : Module {
        protected override void Configure(IServiceCollection services) {
            // A roll entry is made with its roll number. The container gives the presenter of the app scope.
            services.AddFactory<int, RollEntry>((provider, number) =>
                new RollEntry(number, provider.GetRequiredService<CounterPresenter>().Last));
            services.AddPrefabFactory<int, RollEntryView>();
        }
    }
}
