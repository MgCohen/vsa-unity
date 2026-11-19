using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Commands;
using VContainer;
using VcLifetime = VContainer.Lifetime;

namespace Spike {
    // The use cases of open generics (see generics-page.html, tab "Use cases"). Each case runs with three containers:
    // "di" is Microsoft DI with an open registration, "umpire" is the Umpire compose (the scan closes the types),
    // and "vc" is VContainer with an open registration. A check gives its result, or throws when the result is wrong.
    public static class SpikeChecks {
        public static IEnumerable<(string Name, Func<Task<string>> Body)> All(Action<IServiceCollection, AppTypes> compose) {
            var spike = new UmpireCompose(compose);

            // R1: an open command handler over a nested command.
            yield return ("spike:R1:di", () => Di(s => {
                s.AddSingleton<ICommandSender, NoSender>();
                s.AddSingleton(typeof(IHandler<>), typeof(PutHandler<>));
            }, p => One<IHandler<Put<Score>>>(p, "PutHandler<Score>")));
            yield return ("spike:R1:umpire", () => spike.Run(async p => {
                var result = await p.GetRequiredService<ICommandSender>().SendAsync(new Put<Score>(5), CancellationToken.None);
                return result.IsSuccess && result.Value == 5 ? "Put<Score>(5) gives 5" : throw Failed(result.IsSuccess ? "gives " + result.Value : result.Error.Code);
            }));
            yield return ("spike:R1:vc", () => Vc(b => {
                b.Register<ICommandSender, NoSender>(VcLifetime.Singleton);
                b.Register(typeof(PutHandler<>), VcLifetime.Singleton).AsImplementedInterfaces();
            }, r => One<IHandler<Put<Score>>>(r, "PutHandler<Score>")));

            // R2: an open handler, filtered by its constraint.
            yield return ("spike:R2:di", () => Di(s => {
                s.AddSingleton(new Hits());
                s.AddSingleton(typeof(IHandler<>), typeof(TagHandler<>));
            }, p => One<IHandler<AddTag<Flag>>>(p, "TagHandler<Flag>")));
            yield return ("spike:R2:umpire", () => spike.Run(async p => {
                var closed = p.GetRequiredService<AppTypes>().Registered()
                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(TagHandler<>)).Select(Name).ToList();
                if (closed.Count != 1 || closed[0] != "TagHandler<Flag>") {
                    throw Failed("closed: " + string.Join(", ", closed));
                }
                var result = await p.GetRequiredService<ICommandSender>().SendAsync(new AddTag<Flag>(1), CancellationToken.None);
                return result.IsSuccess ? "closed TagHandler<Flag> only; AddTag<Flag> ok" : throw Failed(result.Error.Code);
            }));
            yield return ("spike:R2:vc", () => Vc(b => {
                b.RegisterInstance(new Hits());
                b.Register(typeof(TagHandler<>), VcLifetime.Singleton).AsImplementedInterfaces();
            }, r => One<IHandler<AddTag<Flag>>>(r, "TagHandler<Flag>")));

            // R3: a list of a non-generic service.
            yield return ("spike:R3:di", () => Di(Things, p => ConsumerCount(p.GetRequiredService<Consumer>())));
            yield return ("spike:R3:umpire", () => spike.Run(p => Task.FromResult(ConsumerCount(ActivatorUtilities.CreateInstance<Consumer>(p))), Things));
            yield return ("spike:R3:vc", () => Vc(b => {
                b.RegisterInstance<IThing>(new ThingA());
                b.Register<IThing>(_ => new ThingB(), VcLifetime.Singleton);
                b.Register<Consumer>(VcLifetime.Singleton);
            }, r => ConsumerCount(r.Resolve<Consumer>())));

            // R4: a registry instance made with MakeGenericType, and one instance under two service types.
            yield return ("spike:R4:di", () => Di(Registries, p => RegistryResult(p.GetRequiredService, p.GetRequiredService<BadgeRegistry>())));
            yield return ("spike:R4:umpire", () => spike.Run(p => Task.FromResult(RegistryResult(p.GetRequiredService, p.GetRequiredService<BadgeRegistry>())), Registries));
            yield return ("spike:R4:vc", () => Vc(b => {
                var article = Activator.CreateInstance(typeof(Registry<>).MakeGenericType(typeof(Article)));
                b.RegisterInstance(article, article.GetType());
                b.RegisterInstance(new BadgeRegistry(), typeof(BadgeRegistry)).As(typeof(Registry<Badge>));
            }, r => RegistryResult(t => r.Resolve(t), r.Resolve<BadgeRegistry>())));

            // T1: one event, an open handler and a closed handler.
            yield return ("spike:T1:di", () => Di(s => {
                s.AddSingleton(new Hits());
                s.AddSingleton(typeof(IHandler<>), typeof(LogChanged<>));
                s.AddSingleton<IHandler<Changed<Score>>, ScoreCheck>();
            }, p => Many<IHandler<Changed<Score>>>(p.GetServices<IHandler<Changed<Score>>>(), "LogChanged<Score>", "ScoreCheck")));
            yield return ("spike:T1:umpire", () => spike.Run(async p => {
                var result = await p.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(new Changed<Score>(new Score { Value = 3 }), CancellationToken.None);
                if (!result.IsSuccess) {
                    throw Failed(result.Error.Code);
                }
                return HitsAre(p, "check", "log Score");
            }));
            yield return ("spike:T1:vc", () => Vc(b => {
                b.RegisterInstance(new Hits());
                b.Register(typeof(LogChanged<>), VcLifetime.Singleton).AsImplementedInterfaces();
                b.Register<IHandler<Changed<Score>>, ScoreCheck>(VcLifetime.Singleton);
            }, r => Many<IHandler<Changed<Score>>>(r.Resolve<IEnumerable<IHandler<Changed<Score>>>>(), "LogChanged<Score>", "ScoreCheck")));

            // T2: a hook for each command that implements IAuditable. A container matches exact types only.
            yield return ("spike:T2:di", () => Di(s => {
                s.AddSingleton(new Hits());
                s.AddSingleton<BeforeCommandHook<IAuditable>, AuditHook>();
            }, p => Many<BeforeCommandHook<Rename>>(p.GetServices<BeforeCommandHook<Rename>>(), "AuditHook")));
            yield return ("spike:T2:umpire", () => spike.Run(async p => {
                var commands = p.GetRequiredService<ICommandSender>();
                var renamed = await commands.SendAsync(new Rename("a"), CancellationToken.None);
                var put = await commands.SendAsync(new Put<Score>(1), CancellationToken.None);
                if (!renamed.IsSuccess || !put.IsSuccess) {
                    throw Failed("send failed");
                }
                return HitsAre(p, "audit Rename");
            }));
            yield return ("spike:T2:vc", () => Vc(b => {
                b.RegisterInstance(new Hits());
                b.Register<BeforeCommandHook<IAuditable>, AuditHook>(VcLifetime.Singleton);
            }, r => Many<BeforeCommandHook<Rename>>(r.Resolve<IEnumerable<BeforeCommandHook<Rename>>>(), "AuditHook")));

            // T3: an open validator over a nested command.
            yield return ("spike:T3:di", () => Di(s => s.AddSingleton(typeof(Validator<>), typeof(RangeValidator<>)),
                p => Many<Validator<Put<Score>>>(p.GetServices<Validator<Put<Score>>>(), "RangeValidator<Score>")));
            yield return ("spike:T3:umpire", () => spike.Run(async p => {
                var result = await p.GetRequiredService<ICommandSender>().SendAsync(new Put<Score>(500), CancellationToken.None);
                return !result.IsSuccess && result.Error.Code == "spike.range" ? "Put<Score>(500) refused spike.range" : throw Failed("not refused");
            }));
            yield return ("spike:T3:vc", () => Vc(b => b.Register(typeof(RangeValidator<>), VcLifetime.Singleton).As(typeof(Validator<>)),
                r => Many<Validator<Put<Score>>>(r.Resolve<IEnumerable<Validator<Put<Score>>>>(), "RangeValidator<Score>")));

            // T4: a single resolve by app code, over a class and a struct.
            foreach (var argument in new[] { typeof(Article), typeof(Score) }) {
                var service = typeof(IBox<>).MakeGenericType(argument);
                var expected = "Box<" + argument.Name + ">";
                yield return ("spike:T4:di:" + argument.Name, () => Di(s => s.AddSingleton(typeof(IBox<>), typeof(Box<>)), p => OneOf(p.GetRequiredService(service), expected)));
                yield return ("spike:T4:umpire:" + argument.Name, () => spike.Run(p => Task.FromResult(OneOf(p.GetRequiredService(service), expected))));
                yield return ("spike:T4:vc:" + argument.Name, () => Vc(b => b.Register(typeof(Box<>), VcLifetime.Singleton).As(typeof(IBox<>)), r => OneOf(r.Resolve(service), expected)));
            }

            // T5: all rules for one type, two open and one closed. Score has no closed rule.
            foreach (var argument in new[] { typeof(Article), typeof(Score) }) {
                var service = typeof(IRule<>).MakeGenericType(argument);
                var expected = new List<string> { "BaseRule<" + argument.Name + ">", "CritRule<" + argument.Name + ">" };
                if (argument == typeof(Article)) {
                    expected.Add("ArticleRule");
                }
                yield return ("spike:T5:di:" + argument.Name, () => Di(s => {
                    s.AddSingleton(typeof(IRule<>), typeof(BaseRule<>));
                    s.AddSingleton(typeof(IRule<>), typeof(CritRule<>));
                    s.AddSingleton<IRule<Article>, ArticleRule>();
                }, p => OneAndMany(p.GetRequiredService(service), p.GetServices(service), expected)));
                yield return ("spike:T5:umpire:" + argument.Name, () => spike.Run(p => Task.FromResult(OneAndMany(p.GetRequiredService(service), p.GetServices(service), expected))));
                yield return ("spike:T5:vc:" + argument.Name, () => Vc(b => {
                    b.Register(typeof(BaseRule<>), VcLifetime.Singleton).As(typeof(IRule<>));
                    b.Register(typeof(CritRule<>), VcLifetime.Singleton).As(typeof(IRule<>));
                    b.Register<IRule<Article>, ArticleRule>(VcLifetime.Singleton);
                }, r => OneAndMany(r.Resolve(service), (IEnumerable)r.Resolve(typeof(IEnumerable<>).MakeGenericType(service)), expected)));
            }

            // N1: open classes in a list of a non-generic service.
            yield return ("spike:N1:di", () => Di(s => s.AddSingleton(typeof(IMeter), typeof(Meter<>)),
                p => Many<IMeter>(p.GetServices<IMeter>(), "Meter<Flag>", "Meter<Score>")));
            yield return ("spike:N1:umpire", () => spike.Run(p => Task.FromResult(Many<IMeter>(p.GetServices<IMeter>(), "Meter<Flag>", "Meter<Score>"))));
            yield return ("spike:N1:vc", () => Vc(b => b.Register(typeof(Meter<>), VcLifetime.Singleton).As(typeof(IMeter)),
                r => Many<IMeter>(r.Resolve<IEnumerable<IMeter>>(), "Meter<Flag>", "Meter<Score>")));

            // Edge: one class registered two times for one service. The check gives the single and the list result.
            foreach (var argument in new[] { typeof(Article), typeof(Score) }) {
                var service = typeof(IShelf<>).MakeGenericType(argument);
                var closed = typeof(Shelf<>).MakeGenericType(argument);
                yield return ("spike:Edge:di:" + argument.Name, () => Di(s => {
                    s.AddSingleton(typeof(IShelf<>), typeof(Shelf<>));
                    s.AddSingleton(service, closed);
                }, p => Report(p.GetRequiredService(service), p.GetServices(service))));
                yield return ("spike:Edge:vc:" + argument.Name, () => Vc(b => {
                    b.Register(typeof(Shelf<>), VcLifetime.Singleton).As(typeof(IShelf<>));
                    b.Register(closed, VcLifetime.Singleton).As(service);
                }, r => Report(r.Resolve(service), (IEnumerable)r.Resolve(typeof(IEnumerable<>).MakeGenericType(service)))));
                yield return ("spike:Edge:vc-closed:" + argument.Name, () => Vc(b => {
                    b.Register(closed, VcLifetime.Singleton).As(service);
                    b.Register(closed, VcLifetime.Singleton).As(service);
                }, r => Report(r.Resolve(service), (IEnumerable)r.Resolve(typeof(IEnumerable<>).MakeGenericType(service)))));
            }
            // In Umpire only Article has the second registration (SpikeModule.Configure).
            yield return ("spike:Edge:umpire:Article", () => spike.Run(p => Task.FromResult(Report(p.GetRequiredService<IShelf<Article>>(), p.GetServices<IShelf<Article>>()))));
        }

        // The Umpire compose of the spike module only, with a new container for each check.
        sealed class UmpireCompose {
            readonly Action<IServiceCollection, AppTypes> compose;

            public UmpireCompose(Action<IServiceCollection, AppTypes> compose) {
                this.compose = compose;
            }

            public async Task<string> Run(Func<IServiceProvider, Task<string>> body, Action<IServiceCollection> more = null) {
                var services = new ServiceCollection();
                compose(services, new AppTypes(new Module[] { new SpikeModule() }));
                more?.Invoke(services);
                using (var provider = services.BuildServiceProvider())
                using (var scope = provider.CreateScope()) {
                    return await body(scope.ServiceProvider);
                }
            }
        }

        static void Things(IServiceCollection services) {
            services.AddSingleton<IThing>(new ThingA());
            services.AddSingleton<IThing>(_ => new ThingB());
            services.AddSingleton<Consumer>();
        }

        static void Registries(IServiceCollection services) {
            var article = typeof(Registry<>).MakeGenericType(typeof(Article));
            services.AddSingleton(article, Activator.CreateInstance(article));
            var badge = new BadgeRegistry();
            services.AddSingleton(badge);
            services.AddSingleton<Registry<Badge>>(badge);
        }

        static string RegistryResult(Func<Type, object> resolve, BadgeRegistry badge) {
            var article = resolve(typeof(Registry<Article>));
            var asBase = resolve(typeof(Registry<Badge>));
            if (!ReferenceEquals(asBase, badge)) {
                throw Failed("Registry<Badge> is " + (asBase == null ? "null" : Name(asBase.GetType())) + ", not the BadgeRegistry instance");
            }
            return Name(article.GetType()) + "; Registry<Badge> and BadgeRegistry are one instance";
        }

        static string ConsumerCount(Consumer consumer) =>
            consumer.Count == 2 ? "Consumer gets 2 things" : throw Failed("Consumer gets " + consumer.Count);

        static Task<string> Di(Action<IServiceCollection> register, Func<IServiceProvider, string> body) {
            var services = new ServiceCollection();
            register(services);
            using (var provider = services.BuildServiceProvider()) {
                return Task.FromResult(body(provider));
            }
        }

        static Task<string> Vc(Action<IContainerBuilder> register, Func<IObjectResolver, string> body) {
            var builder = new ContainerBuilder();
            register(builder);
            using (var container = builder.Build()) {
                return Task.FromResult(body(container));
            }
        }

        static string One<T>(IServiceProvider provider, string expected) => OneOf(provider.GetRequiredService<T>(), expected);

        static string One<T>(IObjectResolver resolver, string expected) => OneOf(resolver.Resolve<T>(), expected);

        static string OneOf(object item, string expected) {
            var name = item == null ? "null" : Name(item.GetType());
            return name == expected ? "one " + name : throw Failed("one " + name + ", expected " + expected);
        }

        static string Many<T>(IEnumerable<T> items, params string[] expected) => Many((IEnumerable)items, expected);

        static string Many(IEnumerable items, IReadOnlyCollection<string> expected) {
            var names = items.Cast<object>().Select(i => Name(i.GetType())).ToList();
            var text = "list [" + string.Join(", ", names) + "]";
            return names.OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(n => n, StringComparer.Ordinal))
                ? text
                : throw Failed(text + ", expected [" + string.Join(", ", expected) + "]");
        }

        static string OneAndMany(object one, IEnumerable items, IReadOnlyCollection<string> expected) {
            var name = one == null ? "null" : Name(one.GetType());
            if (!expected.Contains(name)) {
                throw Failed("one " + name + ", expected one of [" + string.Join(", ", expected) + "]");
            }
            return "one " + name + "; " + Many(items, expected);
        }

        static string Report(object one, IEnumerable items) =>
            "one " + Name(one.GetType()) + "; list [" + string.Join(", ", items.Cast<object>().Select(i => Name(i.GetType()))) + "]";

        static string HitsAre(IServiceProvider provider, params string[] expected) {
            var hits = provider.GetRequiredService<Hits>().All();
            var text = "ran [" + string.Join(", ", hits) + "]";
            return hits.OrderBy(h => h, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(h => h, StringComparer.Ordinal))
                ? text
                : throw Failed(text + ", expected [" + string.Join(", ", expected) + "]");
        }

        static Exception Failed(string detail) => new InvalidOperationException("wrong result: " + detail);

        static string Name(Type type) {
            if (!type.IsGenericType) {
                return type.Name;
            }
            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
        }
    }
}
