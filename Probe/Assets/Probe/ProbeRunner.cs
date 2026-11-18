using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Probe.Posts;
using Umpire.Kernels;
using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Requests;
using UnityEngine;

namespace Probe {
    // Runs the probe checks and writes one "CHECK <name> ok|FAIL <detail>" line for each check.
    // The Editor runs it with -executeMethod. In a player or in Play Mode, ShellProbe runs it.
    public static class ProbeRunner {
        // Returns the number of failed checks. The work runs on a thread pool thread, so an await never waits for
        // the Unity main thread while the main thread blocks.
        public static int RunAll() {
            Line("runtime", "ok " + RuntimeName());
            return Task.Run(RunAllAsync).GetAwaiter().GetResult();
        }

        static async Task<int> RunAllAsync() {
            var failed = 0;
            IServiceProvider provider = null;
            failed += await Check("compose", () => {
                var services = new ServiceCollection();
                Compose(services, new ConfigurationBuilder().Build());
                provider = services.BuildServiceProvider();
                return Task.FromResult(services.Count + " services");
            });
            if (provider == null) {
                Line("compose-done", "FAIL compose");
                return failed;
            }
            failed += await Check("modules", () => {
                var names = string.Join(",", ModuleNames(provider.GetRequiredService<AppTypes>()));
                return Task.FromResult(names.Contains("Posts") ? names : throw new Exception("modules: " + names));
            });
            using (var scope = provider.CreateScope()) {
                var scoped = scope.ServiceProvider;
                failed += await Check("request", async () => {
                    var added = await scoped.GetRequiredService<IRequestSender>().SendAsync(new AddPostRequest("first"));
                    return added.IsSuccess && added.Value.Id == 1 ? "id 1" : throw new Exception(Describe(added.Error));
                });
                failed += await Check("event", () => {
                    var published = scoped.GetRequiredService<PostStore>().Published;
                    return Task.FromResult(published == 1 ? "handled 1" : throw new Exception("handled " + published));
                });
                failed += await Check("command", async () => {
                    var renamed = await scoped.GetRequiredService<ICommandSender>()
                        .SendAsync(new RenamePostCommand(1, "renamed"), default);
                    return renamed.IsSuccess && renamed.Value.Title == "renamed" ? "renamed" : throw new Exception(Describe(renamed.Error));
                });
                failed += await Check("presenter", async () => {
                    var presenter = scoped.GetRequiredService<PostsPresenter>();
                    await presenter.LoadAsync();
                    return presenter.Count == 1 ? "count 1" : throw new Exception("count " + presenter.Count + ", error " + presenter.Load.Error);
                });
                // -probeSkip <name> leaves out one check, for example to isolate a hang.
                var skip = Array.IndexOf(Environment.GetCommandLineArgs(), "-probeSkip") is var at && at >= 0
                    ? Environment.GetCommandLineArgs()[at + 1]
                    : null;
                foreach (var (name, body) in Lab.LabChecks.All(scoped).Concat(Lab.VContainerChecks.All()).Concat(Spike.SpikeChecks.All(ComposeSpike))) {
                    if (name == skip) {
                        Line(name, "skipped");
                        continue;
                    }
                    failed += await Check(name, body);
                }
            }
            Line("compose-done", failed == 0 ? "ok" : "FAIL " + failed + " failed");
            return failed;
        }

        // Composes the probe app into a container of the probe, with no shell and no host start.
        public static void Compose(IServiceCollection services, IConfiguration configuration) {
            var app = new ProbeApp();
            app.Pack.CreateHost().Compose(services, configuration, app.Pack.Kernel, new AppTypes(null, typeof(ProbeApp).Assembly));
        }

        // Composes the spike module (Assets/Spike) only, with the pack of the probe app.
        static void ComposeSpike(IServiceCollection services, AppTypes types) {
            var app = new ProbeApp();
            app.Pack.CreateHost().Compose(services, new ConfigurationBuilder().Build(), app.Pack.Kernel, types);
        }

        static IEnumerable<string> ModuleNames(AppTypes types) {
            foreach (var module in types.Modules) {
                yield return module.Name;
            }
        }

        static string Describe(Umpire.Messages.Contracts.Error error) =>
            error == null ? "no error" : error.Code + ": " + error.Message;

        static async Task<int> Check(string name, Func<Task<string>> body) {
            try {
                Line(name, "ok " + await body());
                return 0;
            } catch (Exception e) {
                Line(name, "FAIL " + Flatten(e));
                return 1;
            }
        }

        static string Flatten(Exception e) {
            var text = "";
            for (var inner = e; inner != null; inner = inner.InnerException) {
                text += "[" + inner.GetType().Name + ": " + inner.Message + "] ";
            }
            var stack = e.StackTrace ?? "";
            return (text + "| " + stack).Replace("\r", "").Replace("\n", " <- ");
        }

        public static void Line(string name, string result) {
            var line = "CHECK " + name + " " + result;
            Debug.Log(line);
            Console.WriteLine(line);
        }

        static string RuntimeName() {
#if ENABLE_IL2CPP
            return "il2cpp";
#else
            return "mono";
#endif
        }
    }
}
