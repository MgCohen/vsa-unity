using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Probe.Posts;
using Umpire.Config;
using Umpire.Kernels;
using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;

namespace Probe.Lab {
    // The reflection probes. Each one is a named check of ProbeRunner.
    public static class LabChecks {
        public static IEnumerable<(string Name, Func<Task<string>> Body)> All(IServiceProvider scoped) {
            var types = scoped.GetRequiredService<AppTypes>();
            foreach (var argument in new[] { typeof(Article), typeof(Score), typeof(Mentioned), typeof(int) }) {
                yield return ("open-class:" + argument.Name, () => OpenClass(scoped, types, argument));
            }
            var events = scoped.GetRequiredService<IIntegrationEventPublisher>();
            yield return ("open-handler:Article", () => Publish(events, new Changed<Article>(new Article()), "Article="));
            yield return ("open-handler:Score", () => Publish(events, new Changed<Score>(new Score { Value = 3 }), "Score="));
            yield return ("open-handler:Mentioned", () => Publish(events, new Changed<Mentioned>(new Mentioned()), "Mentioned="));
            yield return ("open-handler:Int32", () => Publish(events, new Changed<int>(5), "Int32=5"));
            yield return ("validator", () => Validator(scoped));
            foreach (var argument in new[] { typeof(Article), typeof(Score), typeof(int) }) {
                yield return ("di-open-generic:" + argument.Name, () => DiOpenGeneric(argument));
            }
            // P2: the same services, registered closed by hand.
            yield return ("di-closed-generic:Score", () => DiClosed(services => services.AddSingleton<IBox<Score>, Box<Score>>(), typeof(IBox<Score>)));
            yield return ("di-closed-generic:Int32", () => DiClosed(services => services.AddSingleton<IBox<int>, Box<int>>(), typeof(IBox<int>)));
            // P2: the open registration, and a closed registration for the struct after it.
            yield return ("di-open-plus-closed:Score", () => DiClosed(services => {
                services.AddSingleton(typeof(IBox<>), typeof(Box<>));
                services.AddSingleton<IBox<Score>, Box<Score>>();
            }, typeof(IBox<Score>)));
            // An [AutoRegister] open interface, resolved through the interface.
            yield return ("auto-interface:Score", () => Task.FromResult(scoped.GetRequiredService<IShelf<Score>>().Name));
            yield return ("auto-interface:Int32", () => Task.FromResult(scoped.GetRequiredService<IShelf<int>>().Name));
            // The same interface over a class with no [AutoClose]. The expected result is "not registered".
            yield return ("auto-unlisted:FooBar", () => Task.FromResult(scoped.GetService<IShelf<FooBar>>() == null
                ? "not registered"
                : throw new Exception("IShelf<FooBar> resolved")));
            // Lists: an open registration and a closed registration for the same service.
            yield return ("list-open-plus-closed:Score", () => List(services => {
                services.AddSingleton(typeof(IBox<>), typeof(Box<>));
                services.AddSingleton<IBox<Score>, Box<Score>>();
            }, typeof(IBox<Score>)));
            yield return ("list-open-plus-closed:Article", () => List(services => {
                services.AddSingleton(typeof(IBox<>), typeof(Box<>));
                services.AddSingleton<IBox<Article>, Box<Article>>();
            }, typeof(IBox<Article>)));
            yield return ("list-open:FooBar", () => List(services => services.AddSingleton(typeof(IBox<>), typeof(Box<>)), typeof(IBox<FooBar>)));
            // The same with Umpire: its closed IShelf<Score>, and an open IShelf<> added after the compose.
            yield return ("list-umpire-plus-open:Score", () => List(services => {
                ProbeRunner.Compose(services, new ConfigurationBuilder().Build());
                services.AddSingleton(typeof(IShelf<>), typeof(Shelf<>));
            }, typeof(IShelf<Score>)));
            // Lists over an open registration: two type arguments, and a struct inside a class argument.
            yield return ("list-open-pair:Article+Score", () => List(services => services.AddSingleton(typeof(IPair<,>), typeof(Pair<,>)), typeof(IPair<Article, Score>)));
            yield return ("list-open-pair:Article+Article", () => List(services => services.AddSingleton(typeof(IPair<,>), typeof(Pair<,>)), typeof(IPair<Article, Article>)));
            yield return ("list-open-nested:List<Score>", () => List(services => services.AddSingleton(typeof(IBox<>), typeof(Box<>)), typeof(IBox<List<Score>>)));
            // MakeGenericType over a struct with no closed use in code, with no DI.
            yield return ("make-generic:Loose", () => {
                var box = Activator.CreateInstance(typeof(Box<>).MakeGenericType(typeof(Loose)));
                return Task.FromResult((string)box.GetType().GetProperty("Name").GetValue(box));
            });
            yield return ("settings", Settings);
            yield return ("settings-code", SettingsCode);
            yield return ("settings-toml", Toml);
            yield return ("settings-toml-case", TomlCase);
            yield return ("logger", () => Logger(scoped));
        }

        // The scan closes Store<> over each type argument. The probe finds the closed type in the scan result,
        // so no code names Store<Score>.
        static Task<string> OpenClass(IServiceProvider services, AppTypes types, Type argument) {
            var closed = types.Registered().FirstOrDefault(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Store<>) && t.GetGenericArguments()[0] == argument);
            if (closed == null) {
                throw new Exception("the scan did not close Store<> over " + argument.Name);
            }
            var store = (IStore)services.GetRequiredService(closed);
            return Task.FromResult(store.Roundtrip());
        }

        static async Task<string> Publish<TEvent>(IIntegrationEventPublisher events, TEvent integrationEvent, string expected)
            where TEvent : IntegrationEvent {
            var result = await events.PublishAsync(integrationEvent, CancellationToken.None);
            if (!result.IsSuccess) {
                throw new Exception(result.Error.Code + ": " + result.Error.Message);
            }
            lock (ChangedHits.All) {
                var hit = ChangedHits.All.FirstOrDefault(h => h.StartsWith(expected, StringComparison.Ordinal));
                return hit ?? throw new Exception("no handler ran; hits: " + string.Join(",", ChangedHits.All));
            }
        }

        static async Task<string> Validator(IServiceProvider services) {
            var result = await services.GetRequiredService<IRequestSender>().SendAsync(new AddPostRequest(""));
            return !result.IsSuccess && result.Error.Code == "posts.title"
                ? "refused " + result.Error.Code
                : throw new Exception("the validator did not run: success " + result.IsSuccess);
        }

        // Resolves IEnumerable<service> and gives the number of items.
        static Task<string> List(Action<IServiceCollection> register, Type service) {
            var services = new ServiceCollection();
            register(services);
            using (var provider = services.BuildServiceProvider()) {
                var items = (System.Collections.IEnumerable)provider.GetRequiredService(typeof(IEnumerable<>).MakeGenericType(service));
                var count = 0;
                foreach (var item in items) {
                    count++;
                }
                return Task.FromResult(count + " items");
            }
        }

        static Task<string> DiClosed(Action<IServiceCollection> register, Type service) {
            var services = new ServiceCollection();
            register(services);
            using (var provider = services.BuildServiceProvider()) {
                var box = provider.GetRequiredService(service);
                return Task.FromResult((string)service.GetProperty("Name").GetValue(box));
            }
        }

        static Task<string> DiOpenGeneric(Type argument) {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(IBox<>), typeof(Box<>));
            using (var provider = services.BuildServiceProvider()) {
                var service = typeof(IBox<>).MakeGenericType(argument);
                var box = provider.GetRequiredService(service);
                return Task.FromResult((string)service.GetProperty("Name").GetValue(box));
            }
        }

        // The compose of UnityHost: the LabSettings asset on the shell wins over ProbeSettings.
        static Task<string> Settings() {
            var services = new ServiceCollection();
            ProbeRunner.Compose(services, new ConfigurationBuilder().Build());
            return Task.FromResult(Expect(LabModule.Bound, "asset 9 b y=3 On"));
        }

        // The settings class in code, with no asset.
        static Task<string> SettingsCode() =>
            Task.FromResult(Expect(new ProbeSettings().Get<LabSettings>(new LabModule()), "hello 7 a x=2 On"));

        // The same settings from TOML text, through Tomlyn (reflection). No file is read.
        static Task<string> Toml() {
            var config = UmpireConfig.Parse(Lines(
                "[Lab]", "Greeting = \"hello\"", "Limit = 7", "Tags = [\"a\"]", "Mode = \"On\"",
                "", "[Lab.Weights]", "x = 2"));
            return Task.FromResult(Expect(config.Get<LabSettings>("Lab"), "hello 7 a x=2 On"));
        }

        // A key with other case than the property name: does Tomlyn bind it?
        static Task<string> TomlCase() {
            var settings = UmpireConfig.Parse(Lines("[Lab]", "greeting = \"lower\"")).Get<LabSettings>("Lab");
            return Task.FromResult(settings == null ? "no table" : "Greeting = " + settings.Greeting);
        }

        static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines) + Environment.NewLine;

        static string Expect(LabSettings s, string expected) {
            if (s == null) {
                throw new Exception("no settings");
            }
            var text = s.Greeting + " " + s.Limit + " " + string.Join("|", s.Tags) + " "
                + (s.Weights == null ? "no-weights" : string.Join(",", s.Weights.Select(p => p.Key + "=" + p.Value))) + " " + s.Mode;
            return text == expected ? text : throw new Exception("bound: " + text);
        }

        static Task<string> Logger(IServiceProvider services) {
            var logger = services.GetRequiredService<ILogger<PostStore>>();
            logger.LogInformation("probe {Value}", 1);
            return Task.FromResult(logger.GetType().Name);
        }
    }
}
