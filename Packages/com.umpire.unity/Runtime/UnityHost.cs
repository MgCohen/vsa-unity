using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umpire.Architecture;
using Umpire.Factories;
using Umpire.Unity.Factories;
using Umpire.Kernels;
using Umpire.Vsa.Presenters;
using UnityEngine;

namespace Umpire.Unity {
    // The Unity host: the Unity log, the settings sources and the presenter scope of the app. One app is one
    // presenter scope: it is live from the start of the host to its end, and the host injects its presenters into
    // the views of the loaded scenes. A presenter gets its events on the thread of the publish, and
    // PresenterComponent moves each Render to the main thread.
    // Without a module list, Umpire scans the loaded assemblies for modules (ScannedModules).
    // To give the modules, derive a host with a public constructor:
    //   public sealed class GameHost : UnityHost { public GameHost() : base(new ScoreModule()) { } }
    public class UnityHost : AppHost {
        IServiceScope presenters;
        ViewInjector views;

        public UnityHost() {
        }

        // Gives the modules of the app (FixedModules). Umpire then reads only the assemblies of these modules.
        protected UnityHost(params Module[] modules) {
            ModuleProvider = new FixedModules(modules);
        }

        // A player adds the arguments of its process after the given ones, so the process arguments win.
        // The Editor does not: its process arguments belong to the Editor, not to the app.
        protected override IConfiguration Configuration(string[] args) =>
            base.Configuration(Application.isEditor
                ? args
                : args.Concat(Environment.GetCommandLineArgs().Skip(1)).ToArray());

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
            // The scene injection binds each bound behaviour through the binder of the factories.
            AddSceneTargets(services, types, FactoryRegistrations.AddBinder(services));
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

        // Registers the T of each Bound<T> class of the app as scoped, if T has no registration and no factory: one
        // target for each app scope, as for a presenter. A scene controller then needs no registration. Configure runs
        // after the modules, so a module registration wins, for example AddFactory<T>() for one target for each copy.
        static void AddSceneTargets(IServiceCollection services, AppTypes types, FactoryRegistry factories) {
            var shell = Shell.Owner != null ? LoadedTypes.Of(Shell.Owner.GetType().Assembly) : Enumerable.Empty<Type>();
            var behaviours = types.Declared()
                .Concat(shell)
                .Where(type => type.IsClass && !type.IsAbstract && typeof(IBoundBehaviour).IsAssignableFrom(type))
                .Distinct();
            foreach (var behaviour in behaviours) {
                var target = TargetOf(behaviour);
                if (target != null && target.IsClass && !target.IsAbstract && !target.ContainsGenericParameters
                    && !factories.Has(target)) {
                    FactoryRegistrations.AddIfAbsent(services, ServiceDescriptor.Scoped(target, target));
                }
            }
        }

        static Type TargetOf(Type behaviour) {
            for (var type = behaviour; type != null; type = type.BaseType) {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Bound<>)) {
                    return type.GetGenericArguments()[0];
                }
            }
            return null;
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
