using System;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

namespace Umpire.Factories {
    // Registers factories: Umpire.Kernels.IFactory<T> (the template interface) and IFactory<TArg, T>.
    // Each factory is transient, so it resolves from the scope that resolves it.
    // The first registration of a type wins: a call does not replace a factory or a product that exists.
    // Each factory binds its product if the product is IBindable<T> (see IBindable).
    // Each registration is a closed type, so IL2CPP closes no open generic at run time.
    public static class FactoryRegistrationExtensions {
        // Registers IFactory<T> and T. If T has no registration, T is transient. Create resolves T.
        public static IServiceCollection AddFactory<T>(this IServiceCollection services) where T : class {
            var registry = AddBinder(services);
            EnsureNoDisposableTransient(AddIfAbsent(services, ServiceDescriptor.Transient<T, T>()));
            AddFactory(services, registry, provider => provider.GetRequiredService<T>());
            return services;
        }

        // Registers IFactory<T>, which calls create. The container does not keep the products, so the caller owns them.
        public static IServiceCollection AddFactory<T>(this IServiceCollection services, Func<IServiceProvider, T> create)
            where T : class {
            if (create == null) {
                throw new ArgumentNullException(nameof(create));
            }
            AddFactory(services, AddBinder(services), create);
            return services;
        }

        // Registers IFactory<TArg, T>, which calls create. The container does not keep the products, so the caller owns them.
        public static IServiceCollection AddFactory<TArg, T>(this IServiceCollection services, Func<IServiceProvider, TArg, T> create)
            where T : class {
            if (create == null) {
                throw new ArgumentNullException(nameof(create));
            }
            AddBinder(services);
            AddIfAbsent(services, ServiceDescriptor.Transient<IFactory<TArg, T>>(provider =>
                new DelegateFactory<TArg, T>(provider, create, provider.GetRequiredService<IBinder>())));
            return services;
        }

        // Registers TFactory as IFactory<T>. If TFactory has no registration, it is transient. To keep one instance,
        // for example a pool, register TFactory as a singleton or scoped before this call.
        public static IServiceCollection AddFactory<T, TFactory>(this IServiceCollection services)
            where T : class where TFactory : class, IFactory<T> {
            var registry = AddBinder(services);
            EnsureNoDisposableTransient(AddIfAbsent(services, ServiceDescriptor.Transient<TFactory, TFactory>()));
            AddFactory(services, registry, provider => provider.GetRequiredService<TFactory>().Create());
            return services;
        }

        static void AddFactory<T>(IServiceCollection services, FactoryRegistry registry, Func<IServiceProvider, T> create)
            where T : class {
            AddIfAbsent(services, ServiceDescriptor.Transient<IFactory<T>>(provider =>
                new DelegateFactory<T>(provider, create, provider.GetRequiredService<IBinder>())));
            registry.Add(typeof(T), provider => provider.GetRequiredService<IFactory<T>>().Create());
        }

        // Adds the registry of the factories and the binder, one time for each service collection.
        static FactoryRegistry AddBinder(IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            foreach (var registered in services) {
                if (registered.ServiceType == typeof(FactoryRegistry) && registered.ImplementationInstance is FactoryRegistry found) {
                    return found;
                }
            }
            var registry = new FactoryRegistry();
            services.Add(ServiceDescriptor.Singleton(registry));
            services.Add(ServiceDescriptor.Transient<IBinder, Binder>());
            return registry;
        }

        // Adds the descriptor if its service type has no registration. Returns the registration that the container uses.
        static ServiceDescriptor AddIfAbsent(IServiceCollection services, ServiceDescriptor descriptor) {
            ServiceDescriptor used = null;
            foreach (var registered in services) {
                if (registered.ServiceType == descriptor.ServiceType && !registered.IsKeyedService) {
                    used = registered;
                }
            }
            if (used != null) {
                return used;
            }
            services.Add(descriptor);
            return descriptor;
        }

        // The container keeps each disposable transient until its scope ends, so each Create would keep one more object.
        static void EnsureNoDisposableTransient(ServiceDescriptor descriptor) {
            if (descriptor.Lifetime != ServiceLifetime.Transient) {
                return;
            }
            var type = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType() ?? descriptor.ServiceType;
            if (typeof(IDisposable).IsAssignableFrom(type) || typeof(IAsyncDisposable).IsAssignableFrom(type)) {
                throw new InvalidOperationException(
                    $"{type.FullName} is a transient that is IDisposable. The scope keeps each disposable transient until " +
                    "it ends, so each Create keeps one more object. Register it as scoped or singleton before AddFactory, " +
                    "or make it not IDisposable.");
            }
        }

        sealed class DelegateFactory<T> : IFactory<T> {
            readonly IServiceProvider provider;
            readonly Func<IServiceProvider, T> create;
            readonly IBinder binder;

            public DelegateFactory(IServiceProvider provider, Func<IServiceProvider, T> create, IBinder binder) {
                this.provider = provider;
                this.create = create;
                this.binder = binder;
            }

            public T Create() {
                var product = create(provider);
                binder.Bind(product);
                return product;
            }
        }

        sealed class DelegateFactory<TArg, T> : IFactory<TArg, T> {
            readonly IServiceProvider provider;
            readonly Func<IServiceProvider, TArg, T> create;
            readonly IBinder binder;

            public DelegateFactory(IServiceProvider provider, Func<IServiceProvider, TArg, T> create, IBinder binder) {
                this.provider = provider;
                this.create = create;
                this.binder = binder;
            }

            public T Create(TArg arg) {
                var product = create(provider, arg);
                binder.Bind(product);
                return product;
            }
        }
    }
}
