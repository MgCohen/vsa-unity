using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umpire.Kernels;

namespace Umpire.Factories.Tests {
    public sealed class Store {
    }

    public sealed class Counter {
        public Counter(Store store) => Store = store;

        public Store Store { get; }

        public int Start { get; set; }
    }

    public sealed class DisposableCounter : IDisposable {
        public void Dispose() {
        }
    }

    public sealed class PooledCounters : IFactory<Counter> {
        readonly Stack<Counter> free = new Stack<Counter>();

        public int Made { get; private set; }

        public Counter Create() {
            Made++;
            return free.Count > 0 ? free.Pop() : new Counter(new Store());
        }
    }

    public sealed class DisposableCounters : IFactory<Counter>, IDisposable {
        public Counter Create() => new Counter(new Store());

        public void Dispose() {
        }
    }

    public sealed class Display : IBindable<Counter> {
        public int Size { get; set; }

        public Counter Target { get; private set; }

        public void Bind(Counter target) => Target = target;
    }

    public sealed class Missing {
    }

    public sealed class Orphan : IBindable<Missing> {
        public void Bind(Missing target) {
        }
    }

    public sealed class TwoTargets : IBindable<Counter>, IBindable<Store> {
        public void Bind(Counter target) {
        }

        public void Bind(Store target) {
        }
    }

    public sealed class BindTests {
        static ServiceProvider Build(Action<IServiceCollection> configure) {
            var services = new ServiceCollection();
            services.AddSingleton<Store>();
            configure(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [Test]
        public void A_product_binds_to_a_target_from_the_factory_of_the_target() {
            using var provider = Build(services => {
                services.AddFactory(p => new Counter(p.GetRequiredService<Store>()) { Start = 3 });
                services.AddFactory<Display>();
            });
            var displays = provider.GetRequiredService<IFactory<Display>>();

            var first = displays.Create();
            var second = displays.Create();

            Assert.That(first.Target.Start, Is.EqualTo(3));
            Assert.That(first.Target, Is.Not.SameAs(second.Target));
        }

        [Test]
        public void With_no_factory_of_the_target_the_scope_gives_the_target() {
            using var provider = Build(services => {
                services.AddScoped<Counter>();
                services.AddFactory<Display>();
            });
            using var scope = provider.CreateScope();
            var displays = scope.ServiceProvider.GetRequiredService<IFactory<Display>>();

            Assert.That(displays.Create().Target, Is.SameAs(displays.Create().Target));
            Assert.That(displays.Create().Target, Is.SameAs(scope.ServiceProvider.GetRequiredService<Counter>()));
        }

        [Test]
        public void A_factory_with_an_argument_binds_its_product() {
            using var provider = Build(services => {
                services.AddFactory<Counter>();
                services.AddFactory<int, Display>((p, size) => new Display { Size = size });
            });

            var display = provider.GetRequiredService<IFactory<int, Display>>().Create(4);

            Assert.That(display.Size, Is.EqualTo(4));
            Assert.That(display.Target, Is.Not.Null);
        }

        [Test]
        public void The_binder_binds_an_object_that_no_factory_made() {
            using var provider = Build(services => services.AddFactory<Counter>());
            var display = new Display();

            provider.GetRequiredService<IBinder>().Bind(display);

            Assert.That(display.Target, Is.Not.Null);
        }

        [Test]
        public void The_binder_does_nothing_for_an_object_that_is_not_bindable() {
            using var provider = Build(services => services.AddFactory<Counter>());

            Assert.DoesNotThrow(() => provider.GetRequiredService<IBinder>().Bind(new Store()));
        }

        [Test]
        public void A_target_with_no_factory_and_no_registration_gives_a_clear_error() {
            using var provider = Build(services => services.AddFactory<Orphan>());

            var error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IFactory<Orphan>>().Create());

            StringAssert.Contains("AddFactory<Missing>()", error.Message);
        }

        [Test]
        public void A_product_that_binds_to_two_types_gives_an_error() {
            using var provider = Build(services => services.AddFactory<Counter>());

            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBinder>().Bind(new TwoTargets()));
        }
    }

    public sealed class FactoryTests {
        static ServiceProvider Build(Action<IServiceCollection> configure) {
            var services = new ServiceCollection();
            services.AddSingleton<Store>();
            configure(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [Test]
        public void AddFactory_registers_the_product_and_Create_resolves_it_with_its_dependencies() {
            using var provider = Build(services => services.AddFactory<Counter>());

            var counter = provider.GetRequiredService<IFactory<Counter>>().Create();

            Assert.That(counter.Store, Is.SameAs(provider.GetRequiredService<Store>()));
        }

        [Test]
        public void Each_Create_gives_a_new_product() {
            using var provider = Build(services => services.AddFactory<Counter>());
            var factory = provider.GetRequiredService<IFactory<Counter>>();

            Assert.That(factory.Create(), Is.Not.SameAs(factory.Create()));
        }

        [Test]
        public void AddFactory_keeps_a_registration_of_the_product_that_exists() {
            using var provider = Build(services => {
                services.AddScoped<Counter>();
                services.AddFactory<Counter>();
            });
            using var scope = provider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IFactory<Counter>>();

            Assert.That(factory.Create(), Is.SameAs(factory.Create()));
            Assert.That(factory.Create(), Is.SameAs(scope.ServiceProvider.GetRequiredService<Counter>()));
        }

        [Test]
        public void A_factory_resolves_from_the_scope_that_resolves_it() {
            using var provider = Build(services => {
                services.AddScoped<Counter>();
                services.AddFactory<Counter>();
            });
            using var first = provider.CreateScope();
            using var second = provider.CreateScope();

            var a = first.ServiceProvider.GetRequiredService<IFactory<Counter>>().Create();
            var b = second.ServiceProvider.GetRequiredService<IFactory<Counter>>().Create();

            Assert.That(a, Is.Not.SameAs(b));
        }

        [Test]
        public void AddFactory_stops_the_start_for_a_disposable_transient_product() {
            var services = new ServiceCollection();

            var error = Assert.Throws<InvalidOperationException>(() => services.AddFactory<DisposableCounter>());

            StringAssert.Contains(nameof(DisposableCounter), error.Message);
        }

        [Test]
        public void AddFactory_accepts_a_disposable_product_that_is_scoped() {
            using var provider = Build(services => {
                services.AddScoped<DisposableCounter>();
                services.AddFactory<DisposableCounter>();
            });
            using var scope = provider.CreateScope();

            Assert.That(scope.ServiceProvider.GetRequiredService<IFactory<DisposableCounter>>().Create(), Is.Not.Null);
        }

        [Test]
        public void AddFactory_with_a_function_calls_it_with_the_provider() {
            using var provider = Build(services =>
                services.AddFactory(p => new Counter(p.GetRequiredService<Store>()) { Start = 7 }));

            var counter = provider.GetRequiredService<IFactory<Counter>>().Create();

            Assert.That(counter.Start, Is.EqualTo(7));
            Assert.That(counter.Store, Is.SameAs(provider.GetRequiredService<Store>()));
            Assert.That(provider.GetService<Counter>(), Is.Null);
        }

        [Test]
        public void AddFactory_with_an_argument_gives_the_argument_to_the_function() {
            using var provider = Build(services =>
                services.AddFactory<int, Counter>((p, start) => new Counter(p.GetRequiredService<Store>()) { Start = start }));

            var counter = provider.GetRequiredService<IFactory<int, Counter>>().Create(10);

            Assert.That(counter.Start, Is.EqualTo(10));
            Assert.That(counter.Store, Is.SameAs(provider.GetRequiredService<Store>()));
        }

        [Test]
        public void AddFactory_with_a_factory_class_uses_that_class() {
            using var provider = Build(services => {
                services.AddSingleton<PooledCounters>();
                services.AddFactory<Counter, PooledCounters>();
            });

            provider.GetRequiredService<IFactory<Counter>>().Create();
            provider.GetRequiredService<IFactory<Counter>>().Create();

            Assert.That(provider.GetRequiredService<PooledCounters>().Made, Is.EqualTo(2));
        }

        [Test]
        public void AddFactory_stops_the_start_for_a_disposable_transient_factory_class() {
            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => services.AddFactory<Counter, DisposableCounters>());
        }

        [Test]
        public void The_first_factory_registration_wins() {
            using var provider = Build(services => {
                services.AddFactory(p => new Counter(p.GetRequiredService<Store>()) { Start = 1 });
                services.AddFactory(p => new Counter(p.GetRequiredService<Store>()) { Start = 2 });
            });

            Assert.That(provider.GetRequiredService<IFactory<Counter>>().Create().Start, Is.EqualTo(1));
        }
    }
}
