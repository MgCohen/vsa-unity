using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace Umpire.Factories {
    // The factories of the service collection, by product type. AddFactory fills it, and the binder uses it.
    // Each entry is a function that the generic AddFactory call made, so IL2CPP closes no type at run time.
    sealed class FactoryRegistry {
        readonly Dictionary<Type, Func<IServiceProvider, object>> creators = new Dictionary<Type, Func<IServiceProvider, object>>();

        readonly Dictionary<Type, Func<IServiceProvider, object, object>> argumentCreators =
            new Dictionary<Type, Func<IServiceProvider, object, object>>();

        public void Add(Type product, Func<IServiceProvider, object> create) {
            if (!creators.ContainsKey(product)) {
                creators.Add(product, create);
            }
        }

        public void Add(Type product, Func<IServiceProvider, object, object> create) {
            if (!argumentCreators.ContainsKey(product)) {
                argumentCreators.Add(product, create);
            }
        }

        public bool TryGet(Type product, out Func<IServiceProvider, object> create) => creators.TryGetValue(product, out create);

        public bool TryGet(Type product, out Func<IServiceProvider, object, object> create) =>
            argumentCreators.TryGetValue(product, out create);
    }

    // Binds each IBindable<T> product to a T from the scope of this binder.
    sealed class Binder : IBinder {
        readonly IServiceProvider provider;
        readonly FactoryRegistry registry;

        public Binder(IServiceProvider provider, FactoryRegistry registry) {
            this.provider = provider;
            this.registry = registry;
        }

        public void Bind(object product) {
            var plan = product == null ? null : BindPlan.Of(product.GetType());
            if (plan == null) {
                return;
            }
            var target = registry.TryGet(plan.Target, out Func<IServiceProvider, object> create)
                ? create(provider)
                : provider.GetService(plan.Target) ?? throw new InvalidOperationException(
                    $"{product.GetType().FullName} binds to {plan.Target.FullName}, but {plan.Target.Name} has no factory " +
                    $"and no registration. Call AddFactory<{plan.Target.Name}>() or register it.");
            plan.Invoke(product, target);
        }

        public void Bind(object product, object arg) {
            var plan = product == null ? null : BindPlan.Of(product.GetType());
            if (plan == null) {
                return;
            }
            if (!registry.TryGet(plan.Target, out Func<IServiceProvider, object, object> create)) {
                throw new InvalidOperationException(
                    $"{product.GetType().FullName} binds to {plan.Target.FullName} with an argument, but {plan.Target.Name} " +
                    $"has no factory with an argument. Call AddFactory<TArg, {plan.Target.Name}>(create).");
            }
            plan.Invoke(product, create(provider, arg));
        }
    }

    // The IBindable<T> of one product type, found one time with reflection. The call uses the MethodInfo of a method
    // that the product class implements, so it needs no MakeGenericMethod.
    sealed class BindPlan {
        static readonly Dictionary<Type, BindPlan> plans = new Dictionary<Type, BindPlan>();

        // The Unity linker keeps a method only if code calls it. No code calls IBindable<T>.Bind directly, so at
        // stripping Low the player lost its metadata, and GetMethod gave null. This call keeps IBindable<T>.Bind and
        // each Bind that implements it. It never runs: the linker cannot know the value of the field.
        static bool never = false;

        static BindPlan() {
            if (never) {
                KeepBind<object>(null, null);
            }
        }

        static void KeepBind<T>(IBindable<T> product, T target) => product.Bind(target);

        readonly MethodInfo bind;

        BindPlan(Type target, MethodInfo bind) {
            Target = target;
            this.bind = bind;
        }

        public Type Target { get; }

        // Null if the type is not IBindable<T>.
        public static BindPlan Of(Type type) {
            lock (plans) {
                if (!plans.TryGetValue(type, out var plan)) {
                    plan = Find(type);
                    plans.Add(type, plan);
                }
                return plan;
            }
        }

        public void Invoke(object product, object target) {
            try {
                bind.Invoke(product, new[] { target });
            } catch (TargetInvocationException e) when (e.InnerException != null) {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        static BindPlan Find(Type type) {
            BindPlan found = null;
            foreach (var contract in type.GetInterfaces()) {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IBindable<>)) {
                    continue;
                }
                if (found != null) {
                    throw new InvalidOperationException(
                        $"{type.FullName} implements IBindable<T> two times. A product binds to one type only.");
                }
                found = new BindPlan(contract.GetGenericArguments()[0], contract.GetMethod(nameof(IBindable<object>.Bind)));
            }
            return found;
        }
    }
}
