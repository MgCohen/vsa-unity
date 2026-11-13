using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;
using UnityEngine;
using VSA.Composition;
using VSA.Contracts.Commands;
using VSA.Module;
using VSA.Unity;

namespace SampleApp {
    // The app class of the sample. VsaHost finds it by the app scan: the App field of VsaHost is empty.
    // It has no [Preserve]: the VsaLinker editor script keeps it for IL2CPP.
    public sealed class SampleApp : VsaApp<SampleApp> {
    }

    // The IL2CPP player check: it runs the sample module through VsaHost, writes CHECK lines to the log, then quits.
    public sealed class Check : PresenterComponent<PingPresenter> {
        int failures;

        protected override void Render() {
        }

        protected override void Start() {
            base.Start();
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            Line("backend", Application.isEditor ? "editor" : "player");
            Line("app", VsaHost.Owner != null ? VsaHost.Owner.AppType.FullName : "none", "SampleApp.SampleApp");
            Line("host", VsaHost.Services?.GetService<IHostLifetime>()?.GetType().Name ?? "none", "UnityLifetime");
            var types = VsaHost.Services.GetRequiredService<ModuleTypes>();
            Line("modules", string.Join(",", types.Modules.Select(m => m.Name)), "Sample");
            Line("typeArguments", string.Join(",", types.TypeArguments().Select(t => t.Name).OrderBy(n => n)), "Deck,Hand");

            var main = Thread.CurrentThread.ManagedThreadId;
            var ping = Task.Run(() => Presenter.PingAsync("far"));
            yield return Wait(() => ping.IsCompleted && Presenter.Last == "far");
            Line("request", ping.IsFaulted ? ping.Exception.InnerException.ToString() : ping.IsCompleted ? ping.Result.Value?.Text + ping.Result.Error?.Message : "not done", "pong far");
            Line("presenter-last", Presenter.Last, "far");
            Line("presenter-event", Presenter.EventThread == main ? "main thread" : "thread " + Presenter.EventThread, "main thread");

            using (var scope = VsaHost.Services.CreateScope()) {
                var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();
                var hand = sender.SendAsync(new Move<Hand>(7), CancellationToken.None);
                var deck = sender.SendAsync(new Move<Deck>(3), CancellationToken.None);
                yield return Wait(() => hand.IsCompleted && deck.IsCompleted);
                Line("struct-argument", Text(hand) + "; " + Text(deck), "moved card 7 to Hand; moved card 3 to Deck");
            }

            var lifetime = VsaHost.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopped.Register(() => Debug.Log("CHECK stopped on quit"));
            Debug.Log(failures == 0 ? "CHECK done ok" : $"CHECK done failures={failures}");
            if (!Application.isEditor) {
                Application.Quit(failures == 0 ? 0 : 1);
            }
        }

        // Waits at most 10 seconds, so a fault gives a FAIL line and not a player that does not stop.
        static IEnumerator Wait(Func<bool> done) {
            var end = Time.realtimeSinceStartup + 10;
            while (!done() && Time.realtimeSinceStartup < end) {
                yield return null;
            }
        }

        static string Text(Task<VSA.Contracts.Result<string>> send) =>
            send.IsFaulted ? send.Exception.InnerException.ToString() : send.IsCompleted ? send.Result.Value ?? send.Result.Error?.Message : "not done";

        void Line(string name, string value, string expected = null) {
            var ok = expected == null || value == expected;
            if (!ok) {
                failures++;
            }
            Debug.Log($"CHECK {name}={value}" + (ok ? "" : $" FAIL expected={expected}"));
        }
    }
}
