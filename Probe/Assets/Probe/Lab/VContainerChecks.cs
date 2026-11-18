using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VContainer;

namespace Probe.Lab {
    // A nested shape: the type parameter is inside the service type argument.
    public sealed class ListBox<T> : IBox<List<T>> {
        public string Name => "List<" + typeof(T).Name + ">";
    }

    // A second implementation of IBox<T>, for the cases with several implementations.
    public sealed class OtherBox<T> : IBox<T> {
        public string Name => "Other " + typeof(T).Name;
    }

    // The DI lab cases (D1 to D12) with VContainer 1.19.0, and no Umpire code.
    public static class VContainerChecks {
        public static IEnumerable<(string Name, Func<Task<string>> Body)> All() {
            foreach (var argument in new[] { typeof(Article), typeof(Score), typeof(int), typeof(Loose) }) {
                yield return ("vc-open:" + argument.Name, () => One(Open, typeof(IBox<>).MakeGenericType(argument)));
            }
            yield return ("vc-closed:Score", () => One(b => b.Register<IBox<Score>, Box<Score>>(Lifetime.Singleton), typeof(IBox<Score>)));
            yield return ("vc-open-plus-closed:Score", () => One(OpenPlusClosedScore, typeof(IBox<Score>)));
            yield return ("vc-list-open-plus-closed:Score", () => List(OpenPlusClosedScore, typeof(IBox<Score>)));
            yield return ("vc-list-open-plus-closed:Article", () => List(b => {
                Open(b);
                b.Register<IBox<Article>, Box<Article>>(Lifetime.Singleton);
            }, typeof(IBox<Article>)));
            yield return ("vc-list-open:FooBar", () => List(Open, typeof(IBox<FooBar>)));
            yield return ("vc-list-open:Score", () => List(Open, typeof(IBox<Score>)));
            yield return ("vc-open-pair:Article+Score", () => One(OpenPair, typeof(IPair<Article, Score>)));
            yield return ("vc-list-open-pair:Article+Score", () => List(OpenPair, typeof(IPair<Article, Score>)));
            // Several implementations of one service: get one, get many.
            yield return ("vc-multi-closed:Score", () => OneAndList(b => {
                b.Register<IBox<Score>, Box<Score>>(Lifetime.Singleton);
                b.Register<IBox<Score>, OtherBox<Score>>(Lifetime.Singleton);
            }, typeof(IBox<Score>)));
            yield return ("vc-same-twice:Score", () => OneAndList(b => {
                b.Register<IBox<Score>, Box<Score>>(Lifetime.Singleton);
                b.Register<IBox<Score>, Box<Score>>(Lifetime.Singleton);
            }, typeof(IBox<Score>)));
            yield return ("vc-multi-open:Score", () => OneAndList(b => {
                Open(b);
                b.Register(typeof(OtherBox<>), Lifetime.Singleton).As(typeof(IBox<>));
            }, typeof(IBox<Score>)));
            // The same cases with Microsoft DI, for comparison.
            yield return ("di-multi-closed:Score", () => DiOneAndList(s => {
                s.AddSingleton<IBox<Score>, Box<Score>>();
                s.AddSingleton<IBox<Score>, OtherBox<Score>>();
            }, typeof(IBox<Score>)));
            yield return ("di-same-twice:Score", () => DiOneAndList(s => {
                s.AddSingleton<IBox<Score>, Box<Score>>();
                s.AddSingleton<IBox<Score>, Box<Score>>();
            }, typeof(IBox<Score>)));
            yield return ("vc-nested:List<Score>", () => One(
                b => b.Register(typeof(ListBox<>), Lifetime.Singleton).AsImplementedInterfaces(),
                typeof(IBox<List<Score>>)));
        }

        static void Open(IContainerBuilder builder) =>
            builder.Register(typeof(Box<>), Lifetime.Singleton).As(typeof(IBox<>));

        static void OpenPair(IContainerBuilder builder) =>
            builder.Register(typeof(Pair<,>), Lifetime.Singleton).As(typeof(IPair<,>));

        static void OpenPlusClosedScore(IContainerBuilder builder) {
            Open(builder);
            builder.Register<IBox<Score>, Box<Score>>(Lifetime.Singleton);
        }

        static Task<string> One(Action<IContainerBuilder> register, Type service) {
            var builder = new ContainerBuilder();
            register(builder);
            using (var container = builder.Build()) {
                var item = container.Resolve(service);
                return Task.FromResult((string)item.GetType().GetProperty("Name").GetValue(item));
            }
        }

        // Gives the implementation of a single resolve and the implementations of a list resolve.
        static Task<string> OneAndList(Action<IContainerBuilder> register, Type service) {
            var builder = new ContainerBuilder();
            register(builder);
            using (var container = builder.Build()) {
                var one = container.Resolve(service).GetType().Name;
                var many = ((System.Collections.IEnumerable)container.Resolve(typeof(IEnumerable<>).MakeGenericType(service))).Cast<object>().Select(i => i.GetType().Name);
                return Task.FromResult("one " + one + "; list [" + string.Join(", ", many) + "]");
            }
        }

        static Task<string> DiOneAndList(Action<IServiceCollection> register, Type service) {
            var services = new ServiceCollection();
            register(services);
            using (var provider = services.BuildServiceProvider()) {
                var one = provider.GetRequiredService(service).GetType().Name;
                var many = provider.GetServices(service).Select(i => i.GetType().Name);
                return Task.FromResult("one " + one + "; list [" + string.Join(", ", many) + "]");
            }
        }

        static Task<string> List(Action<IContainerBuilder> register, Type service) {
            var builder = new ContainerBuilder();
            register(builder);
            using (var container = builder.Build()) {
                var items = ((System.Collections.IEnumerable)container.Resolve(typeof(IEnumerable<>).MakeGenericType(service))).Cast<object>().ToList();
                return Task.FromResult(items.Count + " items");
            }
        }
    }
}
