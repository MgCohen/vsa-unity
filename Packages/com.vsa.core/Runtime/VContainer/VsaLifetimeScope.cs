using System;
using System.Collections.Generic;
using Umpire.Kernels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Umpire.Unity.VContainer {
    // The VContainer root scope of a VSA app. It replaces the shell: the scope starts the app, so the start order
    // does not depend on the execution order of two Awake methods. The consumer closes it over its pack:
    //   public sealed class GameScope : VsaLifetimeScope<Vsa<GameHost>> { }
    // The host of the pack must be UnityHost or a class derived from it, because UnityHost adds the imports.
    //
    // Build order:
    //   1. Configure: registers VsaScope, then ConfigureScope.
    //   2. VContainer builds the root scope.
    //   3. Build callback: resolves the imports, starts the app, then injects autoInjectGameObjects.
    //   4. VContainer starts the entry points (IInitializable, IStartable). The app runs at this step.
    // OnDestroy disposes the VContainer scope first, then stops the app.
    public abstract class VsaLifetimeScope<TPack> : LifetimeScope where TPack : Pack, new() {
        [SerializeField, Tooltip("The arguments of App.Start.")]
        string[] arguments = Array.Empty<string>();

        ScopeApp app;

        public string[] Arguments {
            get => arguments;
            set => arguments = value ?? Array.Empty<string>();
        }

        // True from the build of this scope until OnDestroy, if this scope started the app.
        public bool StartedTheApp => app != null;

        // True while the app of this scope runs.
        public bool AppIsRunning => app != null && app.State == AppState.Running;

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

        // Names the services of this scope that the VSA app gets.
        protected virtual void Import(VsaImports imports) {
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            StopApp();
        }

        void StartApp(IObjectResolver resolver) {
            var started = new ScopeApp();
            Shell.Claim(started, name);
            var imports = new VsaImports();
            Import(imports);
            var instances = new List<(Type, object)>();
            foreach (var type in imports.Types) {
                instances.Add((type, resolver.Resolve(type)));
            }
            HostImports.Give(instances);
            bool taken;
            try {
                started.Start(arguments);
            } catch {
                Shell.Release(started);
                throw;
            } finally {
                taken = HostImports.IsTaken;
                HostImports.Take();
            }
            app = started;
            if (!taken && instances.Count > 0) {
                StopApp();
                throw new InvalidOperationException(
                    $"{typeof(TPack).Name} has imports, but its host does not derive from UnityHost.");
            }
        }

        void StopApp() {
            var stopped = app;
            if (stopped == null) {
                return;
            }
            app = null;
            try {
                stopped.Stop();
            } finally {
                Shell.Release(stopped);
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

        sealed class ScopeApp : App<TPack> {
        }
    }
}
