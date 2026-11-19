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
    // The Unity host: the Unity log and the settings sources. A presenter gets its events on the thread of the
    // publish, and PresenterComponent moves each Render to the main thread.
    // Without a module list, Umpire finds the modules in the loaded assemblies that reference Umpire.Kernels.
    // To give the modules, derive a host with a public constructor:
    //   public sealed class GameHost : UnityHost { public GameHost() : base(new ScoreModule()) { } }
    public class UnityHost : AppHost {
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
