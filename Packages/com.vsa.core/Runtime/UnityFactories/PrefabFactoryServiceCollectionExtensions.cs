using System;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Factories;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Umpire.Unity.Factories {
    // Registers prefab factories. The first registration of a type wins, as for AddFactory.
    public static class PrefabFactoryServiceCollectionExtensions {
        // Registers IPrefabFactory<TComponent>. If TComponent is IBindable<T> and T has no registration, T is transient.
        // A scanned presenter is already scoped when a module runs, so it stays scoped and shared.
        public static IServiceCollection AddPrefabFactory<TComponent>(this IServiceCollection services)
            where TComponent : Component {
            FactoryRegistrations.AddBinder(services);
            var target = TargetOf(typeof(TComponent));
            if (target != null) {
                FactoryRegistrations.AddProduct(services, target);
            }
            FactoryRegistrations.AddIfAbsent(services,
                ServiceDescriptor.Transient<IPrefabFactory<TComponent>, PrefabFactory<TComponent>>());
            return services;
        }

        // Registers IPrefabFactory<TArg, TComponent>. The target of TComponent needs AddFactory<TArg, T>(create).
        public static IServiceCollection AddPrefabFactory<TArg, TComponent>(this IServiceCollection services)
            where TComponent : Component {
            FactoryRegistrations.AddBinder(services);
            FactoryRegistrations.AddIfAbsent(services,
                ServiceDescriptor.Transient<IPrefabFactory<TArg, TComponent>, PrefabFactory<TArg, TComponent>>());
            return services;
        }

        // The T of IBindable<T> on the type, else null. It runs one time, at the registration.
        static Type TargetOf(Type type) {
            foreach (var contract in type.GetInterfaces()) {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IBindable<>)) {
                    return contract.GetGenericArguments()[0];
                }
            }
            return null;
        }

        // Binds each behaviour of the copy. Unity gives null for a component with a missing script.
        internal static void BindAll(IBinder binder, Component copy, Component skip) {
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) {
                if (behaviour != null && behaviour != skip) {
                    binder.Bind(behaviour);
                }
            }
        }

        internal static TComponent Copy<TComponent>(TComponent prefab, Transform parent) where TComponent : Component {
            if (prefab == null) {
                throw new ArgumentNullException(nameof(prefab));
            }
            return Object.Instantiate(prefab, parent);
        }

        sealed class PrefabFactory<TComponent> : IPrefabFactory<TComponent> where TComponent : Component {
            readonly IBinder binder;

            public PrefabFactory(IBinder binder) => this.binder = binder;

            public TComponent Create(TComponent prefab, Transform parent = null) {
                var copy = Copy(prefab, parent);
                BindAll(binder, copy, null);
                return copy;
            }
        }

        sealed class PrefabFactory<TArg, TComponent> : IPrefabFactory<TArg, TComponent> where TComponent : Component {
            readonly IBinder binder;

            public PrefabFactory(IBinder binder) => this.binder = binder;

            public TComponent Create(TComponent prefab, TArg arg, Transform parent = null) {
                var copy = Copy(prefab, parent);
                binder.Bind(copy, arg);
                BindAll(binder, copy, copy);
                return copy;
            }
        }
    }
}
