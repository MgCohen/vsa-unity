using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VContainer;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Unity.VContainer {
    // The Out direction: a VContainer scope resolves VSA services.
    //   builder.RegisterVsa<CounterPresenter>();
    // VContainer resolves the type from the VsaScope of the LifetimeScope that resolves it. Thus one LifetimeScope
    // shares one presenter, and two LifetimeScopes get two presenters.
    public static class VsaContainerBuilderExtensions {
        // Registers a VSA service in this scope. The VSA registration sets the lifetime.
        public static RegistrationBuilder RegisterVsa<T>(this IContainerBuilder builder) where T : class =>
            builder.RegisterVsa(typeof(T));

        public static RegistrationBuilder RegisterVsa(this IContainerBuilder builder, Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (!builder.Exists(typeof(VsaScope), findParentScopes: true)) {
                builder.Register<VsaScope>(Lifetime.Scoped);
            }
            return builder.Register(new VsaRegistrationBuilder(type));
        }

        // Registers each presenter of the modules. Give the modules of the app, for example the modules of Modules(...).
        public static void RegisterVsaPresenters(this IContainerBuilder builder, params VSA.Module.Module[] modules) {
            var presenters = new ModuleTypes(modules).Implementations(typeof(Presenter))
                .Select(found => found.Implementation)
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .Distinct();
            foreach (var type in presenters) {
                builder.RegisterVsa(type);
            }
        }

        // Transient in VContainer: VContainer does not keep or dispose the instance. The VSA scope does both.
        sealed class VsaRegistrationBuilder : RegistrationBuilder {
            public VsaRegistrationBuilder(Type type) : base(type, Lifetime.Transient) {
            }

            public override Registration Build() =>
                new Registration(ImplementationType, Lifetime, InterfaceTypes, new FromVsaScope(ImplementationType), Key);
        }

        sealed class FromVsaScope : IInstanceProvider {
            readonly Type type;

            public FromVsaScope(Type type) => this.type = type;

            public object SpawnInstance(IObjectResolver resolver) =>
                resolver.Resolve<VsaScope>().Services.GetRequiredService(type);
        }
    }
}
