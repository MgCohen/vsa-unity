using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Architecture;
using Umpire.Kernels;
using Umpire.Vsa.Presenters;
using UnityEngine;

namespace Umpire.Unity {
    // The Unity host: the Unity log, the settings sources and the presenter scope of the app. One app is one
    // presenter scope: it is live from the start of the host to its end, and the host injects its presenters into
    // the views of the loaded scenes. A presenter gets its events on the thread of the publish, and
    // PresenterComponent moves each Render to the main thread.
    // Without a module list, Umpire finds the modules in the loaded assemblies that reference Umpire.Kernels.
    // To give the modules, derive a host with a public constructor:
    //   public sealed class GameHost : UnityHost { public GameHost() : base(new ScoreModule()) { } }
    public class UnityHost : AppHost {
        IServiceScope presenters;
        ViewInjector views;

        public UnityHost() {
        }

        // Gives the modules of the app. Umpire then reads only the assemblies of these modules.
        protected UnityHost(params Module[] modules) {
            Modules = modules;
        }

        // Unity has no .umpireconfig in its base folder. The host asks the assets on the shell first,
        // then the UnitySettings classes of the app, in name order.
        protected sealed override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) =>
            new ISettingsProvider[] { new AssetSettings() }
                .Concat(SettingsClasses(types).Select(type => (ISettingsProvider)Activator.CreateInstance(type)))
                .ToList();

        protected sealed override void Configure(IServiceCollection services, AppTypes types) {
            services.AddLogging(logging => logging.AddProvider(new UnityLogProvider()));
            foreach (var (type, instance) in HostImports.Take()) {
                services.AddSingleton(type, instance);
            }
        }

        // Makes the presenter scope of the app, makes it live, and injects the views. It runs before App.OnStart.
        public sealed override void Start(IServiceProvider services, AppStop stop) {
            presenters = services.CreateScope();
            presenters.ServiceProvider.GetRequiredService<PresenterScope>();
            views = new ViewInjector(presenters.ServiceProvider);
        }

        // Stops the injection and ends the presenter scope before the services of the app.
        public sealed override void Dispose() {
            try {
                views?.Dispose();
                presenters?.Dispose();
            } finally {
                views = null;
                presenters = null;
                base.Dispose();
            }
        }

        // The UnitySettings classes of the modules and of the shell assembly. The app class of a shell is in this
        // package, so AppTypes does not read the assembly of the shell.
        static IEnumerable<Type> SettingsClasses(AppTypes types) {
            var shell = Shell.Owner != null
                ? LoadedTypes.Of(Shell.Owner.GetType().Assembly)
                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(UnitySettings)))
                : Enumerable.Empty<Type>();
            return types.ConcreteTypesOf(typeof(UnitySettings))
                .Concat(shell)
                .Distinct()
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }
    }
}
