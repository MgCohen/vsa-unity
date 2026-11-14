using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;

namespace VSA.Unity.VContainer {
    // The In direction: the VSA app gets services of the VContainer root scope.
    //   protected override void Import(VsaImports imports) => imports.Add<IAudio>();
    // VsaLifetimeScope resolves each type one time, after VContainer builds the root scope and before the app starts.
    // The app gets the instance as a singleton. VContainer owns it, so the app does not dispose it.
    public sealed class VsaImports {
        readonly List<Type> types = new List<Type>();

        public VsaImports Add<T>() where T : class => Add(typeof(T));

        public VsaImports Add(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (!types.Contains(type)) {
                types.Add(type);
            }
            return this;
        }

        internal IReadOnlyList<Type> Types => types;
    }

    public static class VContainerVsaBuilderExtensions {
        // Adds the imports of the VsaLifetimeScope that starts the app:
        //   protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseUnityHost().UseVContainer();
        // If no VsaLifetimeScope starts the app, for example a VsaHost, it adds nothing.
        public static VsaBuilder UseVContainer(this VsaBuilder builder) => builder.Services(services => {
            var imports = PendingImports.Take();
            if (imports == null) {
                return;
            }
            foreach (var (type, instance) in imports) {
                services.AddSingleton(type, instance);
            }
        });
    }

    // VsaApp<TApp>.Run is static and makes the app, so the scope gives the imports through this slot.
    // The slot has a value only in VsaLifetimeScope.StartApp, on the main thread.
    static class PendingImports {
        static IReadOnlyList<(Type, object)> pending;

        internal static bool IsTaken => pending == null;

        internal static void Give(IReadOnlyList<(Type, object)> imports) => pending = imports;

        internal static IReadOnlyList<(Type, object)> Take() {
            var imports = pending;
            pending = null;
            return imports;
        }
    }
}
