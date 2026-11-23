using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umpire.Kernels;

namespace Umpire.Factories.Tests {
    // A module file has both usings: Umpire.Kernels (Module, IFactory<T>, the template AddFactory) and
    // Umpire.Factories. This file compiles only if the two namespaces have no name clash.
    public sealed class TemplateFactoryTests {
        [Test]
        public void Both_namespaces_work_in_one_file() {
            var services = new ServiceCollection();
            services.AddSingleton<Store>();
            services.AddFactory<Counter>();
            services.AddFactory<int, Display>((p, size) => new Display { Size = size });
            using var provider = services.BuildServiceProvider();

            IFactory<Counter> counters = provider.GetRequiredService<IFactory<Counter>>();
            IFactory<int, Display> displays = provider.GetRequiredService<IFactory<int, Display>>();

            Assert.That(counters.Create().Store, Is.Not.Null);
            Assert.That(displays.Create(2).Size, Is.EqualTo(2));
        }

        [Test]
        public void A_lambda_with_one_parameter_calls_the_template_AddFactory() {
            var services = new ServiceCollection();
            services.AddSingleton<Store>();

            services.AddFactory<Counter, Store>(store => new Counter(store) { Start = 9 });
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IFactory<Counter>>().Create().Start, Is.EqualTo(9));
            Assert.That(provider.GetService<IBinder>(), Is.Null);
        }

        [Test]
        public void A_lambda_with_two_parameters_calls_the_AddFactory_with_an_argument() {
            var services = new ServiceCollection();
            services.AddSingleton<Store>();

            services.AddFactory<int, Counter>((p, start) => new Counter(p.GetRequiredService<Store>()) { Start = start });
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IFactory<int, Counter>>().Create(4).Start, Is.EqualTo(4));
            Assert.That(provider.GetService<IFactory<Counter>>(), Is.Null);
        }
    }
}
