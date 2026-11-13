using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;
using UnityEngine;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Module;
using VSA.Unity;

namespace SampleApp {
    public sealed class Check : PresenterComponent<PingPresenter> {
        const float WaitSeconds = 10;

        int failures;

        protected override void Render() {
        }

        protected override void Start() {
            base.Start();
            StartCoroutine(Run());
        }

        static IEnumerator WaitAtMostTenSeconds(Func<bool> done) {
            var end = Time.realtimeSinceStartup + WaitSeconds;
            while (!done() && Time.realtimeSinceStartup < end) {
                yield return null;
            }
        }

        static string TextOf<T>(Task<Result<T>> send, Func<T, string> text) =>
            send.IsFaulted ? send.Exception.InnerException.ToString()
            : !send.IsCompleted ? "not done"
            : send.Result.IsSuccess ? text(send.Result.Value)
            : send.Result.Error.Message;

        IEnumerator Run() {
            Line("backend", Application.isEditor ? "editor" : "player");
            Line("app", VsaHost.Owner.AppType.FullName, "SampleApp.SampleApp");
            Line("host", VsaHost.Services.GetRequiredService<IHostLifetime>().GetType().Name, "UnityLifetime");
            var types = VsaHost.Services.GetRequiredService<ModuleTypes>();
            Line("modules", string.Join(",", types.Modules.Select(m => m.Name)), "Sample");
            var typeArguments = types.TypeArguments().Select(t => t.Name).OrderBy(n => n);
            Line("typeArguments", string.Join(",", typeArguments), "Deck,Hand");
            yield return SendFromAnotherThread();
            yield return SendStructArguments();
            var lifetime = VsaHost.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopped.Register(() => Debug.Log("CHECK stopped on quit"));
            Debug.Log(failures == 0 ? "CHECK done ok" : $"CHECK done failures={failures}");
            if (!Application.isEditor) {
                Application.Quit(failures == 0 ? 0 : 1);
            }
        }

        IEnumerator SendFromAnotherThread() {
            var main = Thread.CurrentThread.ManagedThreadId;
            var ping = Task.Run(() => Presenter.PingAsync("far"));
            yield return WaitAtMostTenSeconds(() => ping.IsCompleted && Presenter.Last == "far");
            Line("request", TextOf(ping, pong => pong.Text), "pong far");
            Line("presenter-last", Presenter.Last, "far");
            var thread = Presenter.EventThread == main ? "main thread" : "thread " + Presenter.EventThread;
            Line("presenter-event", thread, "main thread");
        }

        IEnumerator SendStructArguments() {
            using var scope = VsaHost.Services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();
            var hand = sender.SendAsync(new Move<Hand>(7), CancellationToken.None);
            var deck = sender.SendAsync(new Move<Deck>(3), CancellationToken.None);
            yield return WaitAtMostTenSeconds(() => hand.IsCompleted && deck.IsCompleted);
            var moved = TextOf(hand, text => text) + "; " + TextOf(deck, text => text);
            Line("struct-argument", moved, "moved card 7 to Hand; moved card 3 to Deck");
        }

        void Line(string name, string value, string expected = null) {
            var ok = expected == null || value == expected;
            if (!ok) {
                failures++;
            }
            Debug.Log($"CHECK {name}={value}" + (ok ? "" : $" FAIL expected={expected}"));
        }
    }
}
