using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VSA.Composition;

namespace VSA.Unity.VContainer {
    // The VContainer root scope of a VSA app. It replaces VsaHost<TApp>: the scope starts the app, so the start order
    // does not depend on the execution order of two Awake methods. The consumer closes it over its app class:
    //   public sealed class GameScope : VsaLifetimeScope<MyApp> { }
    //
    // Build order:
    //   1. Configure: registers VsaScope, then ConfigureScope.
    //   2. VContainer builds the root scope.
    //   3. Build callback: resolves the imports, runs TApp, then injects autoInjectGameObjects.
    //   4. VContainer starts the entry points (IInitializable, IStartable). The app runs at this step.
    // OnDestroy disposes the VContainer scope first, then stops the app.
    public abstract class VsaLifetimeScope<TApp> : LifetimeScope where TApp : VsaApp<TApp>, new() {
        [SerializeField, Tooltip("The arguments of Run, for example --Modules:Counter:Max=5.")]
        string[] arguments = Array.Empty<string>();

        bool started;

        public string[] Arguments {
            get => arguments;
            set => arguments = value ?? Array.Empty<string>();
        }

        // True from the build of this scope until OnDestroy, if this scope started the app.
        public bool StartedTheApp => started;

        protected sealed override void Configure(IContainerBuilder builder) {
            // VContainer injects autoInjectGameObjects before the build callbacks, so before the app runs.
            // This scope injects them after the app starts.
            var targets = autoInjectGameObjects;
            autoInjectGameObjects = null;
            builder.Register<VsaScope>(Lifetime.Scoped);
            builder.RegisterBuildCallback(resolver => {
                StartApp(resolver);
                autoInjectGameObjects = targets;
                InjectAll(resolver, targets);
            });
            ConfigureScope(builder);
        }

        // Registers the services of the root scope, as Configure of a LifetimeScope.
        protected virtual void ConfigureScope(IContainerBuilder builder) {
        }

        // Names the services of this scope that the VSA app gets. The app must call UseVContainer() in Builder.
        protected virtual void Import(VsaImports imports) {
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (started) {
                started = false;
                VsaApp<TApp>.Stop();
            }
        }

        void StartApp(IObjectResolver resolver) {
            if (VsaHost.IsRunning) {
                throw new InvalidOperationException(
                    $"{name}: a VSA app runs already, so {typeof(TApp).Name} cannot start. " +
                    "Remove the VsaHost, or use RegisterVsa in a LifetimeScope.");
            }
            var imports = new VsaImports();
            Import(imports);
            var instances = new List<(Type, object)>();
            foreach (var type in imports.Types) {
                instances.Add((type, resolver.Resolve(type)));
            }
            PendingImports.Give(instances);
            bool taken;
            try {
                VsaApp<TApp>.Run(arguments);
            } finally {
                taken = PendingImports.IsTaken;
                PendingImports.Take();
            }
            started = true;
            if (!taken && instances.Count > 0) {
                started = false;
                VsaApp<TApp>.Stop();
                throw new InvalidOperationException(
                    $"{typeof(TApp).Name} has imports, but its Builder does not call UseVContainer().");
            }
        }

        static void InjectAll(IObjectResolver resolver, List<GameObject> targets) {
            if (targets == null) {
                return;
            }
            foreach (var target in targets) {
                if (target != null) {
                    resolver.InjectGameObject(target);
                }
            }
        }
    }
}
