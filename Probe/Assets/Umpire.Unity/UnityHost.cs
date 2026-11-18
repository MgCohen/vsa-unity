using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Kernels;
using Umpire.Vsa.Presenters;
using UnityEngine;

namespace Umpire.Unity {
    // The Unity host: the Unity log, the main thread for presenter events, and the settings sources.
    public sealed class UnityHost : AppHost {
        // Unity has no .umpireconfig in its base folder. The host asks the assets on the shell first,
        // then the UnitySettings classes of the app, in name order.
        protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) =>
            new ISettingsProvider[] { new AssetSettings() }
                .Concat(types.ConcreteTypesOf(typeof(UnitySettings))
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .Select(type => (ISettingsProvider)Activator.CreateInstance(type)))
                .ToList();

        protected override void Configure(IServiceCollection services, AppTypes types) {
            services.AddLogging(logging => logging.AddProvider(new UnityLogProvider()));
            services.AddSingleton<LivePresenters.Dispatch>(async work => {
                await Awaitable.MainThreadAsync();
                await work();
            });
        }
    }
}
