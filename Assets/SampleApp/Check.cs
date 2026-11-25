using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Unity;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Presenters;
using UnityEngine;

namespace SampleApp {
    public sealed class Check : PresenterComponent<PingPresenter> {
        const float WaitSeconds = 10;

        int failures;

        protected override void Render() {
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

        void Start() {
            StartCoroutine(Run());
        }

        IEnumerator Run() {
            Line("backend", Application.isEditor ? "editor" : "player");
            Line("shell", Shell.Owner.GetType().FullName, "SampleApp.SampleShell");
            CheckArguments();
            var types = Shell.Services.GetRequiredService<AppTypes>();
            Line("modules", string.Join(",", types.Modules.Select(m => m.Name)), "Sample");
            var moves = types.Registered()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("MoveHandler"))
                .Select(t => t.GetGenericArguments()[0].Name)
                .OrderBy(n => n);
            Line("typeArguments", string.Join(",", moves), "Deck,Hand");
            yield return SendFromAnotherThread();
            yield return SendStructArguments();
            foreach (var (name, value, expected) in FactoryCheck.Run()) {
                Line(name, value, expected);
            }
            Debug.Log(failures == 0 ? "CHECK done ok" : $"CHECK done failures={failures}");
            if (!Application.isEditor) {
                Application.Quit(failures == 0 ? 0 : 1);
            }
        }

        void CheckArguments() {
            if (Application.isEditor) {
                return;
            }
            var configuration = Shell.Services.GetRequiredService<IConfiguration>();
            Line("argument", configuration["Probe:Value"] ?? "null", "42");
            Line("argument-order", configuration["Probe:Order"] ?? "null", "process");
            Line("unity-argument", configuration["batchmode"] ?? "null", "null");
        }

        IEnumerator SendFromAnotherThread() {
            var main = Thread.CurrentThread.ManagedThreadId;
            var ping = Task.Run(() => Presenter.PingAsync("far"));
            yield return WaitAtMostTenSeconds(() => ping.IsCompleted && Presenter.Last == "far");
            Line("request", TextOf(ping, pong => pong.Text), "pong far");
            Line("presenter-last", Presenter.Last, "far");
            var thread = Presenter.EventThread == main ? "main thread" : "the publish thread";
            Line("presenter-event", thread, "the publish thread");
        }

        IEnumerator SendStructArguments() {
            using var scope = Shell.Services.CreateScope();
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
