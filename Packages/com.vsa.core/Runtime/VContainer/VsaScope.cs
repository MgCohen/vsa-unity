using System;
using Microsoft.Extensions.DependencyInjection;

namespace VSA.Unity.VContainer {
    // The VSA scope of one VContainer scope. VContainer makes one for each LifetimeScope (Lifetime.Scoped)
    // and disposes it with that LifetimeScope. The VSA scope starts at the first use, so a scope can be
    // built before the app runs.
    public sealed class VsaScope : IDisposable {
        IServiceScope scope;
        bool disposed;

        // The services of the VSA scope. Scoped VSA services, for example presenters, are one for each VsaScope.
        public IServiceProvider Services {
            get {
                if (disposed) {
                    throw new ObjectDisposedException(nameof(VsaScope));
                }
                if (scope == null) {
                    var root = VsaHost.Services ?? throw new InvalidOperationException(
                        "No VSA app runs. Start the app with VsaLifetimeScope<TApp> or VsaHost<TApp> " +
                        "before a VContainer scope resolves a VSA service.");
                    scope = root.CreateScope();
                }
                return scope.ServiceProvider;
            }
        }

        public void Dispose() {
            if (disposed) {
                return;
            }
            disposed = true;
            try {
                scope?.Dispose();
            } catch (ObjectDisposedException) {
                // The app stopped before this scope, for example on Application.quitting.
            }
            scope = null;
        }
    }
}
