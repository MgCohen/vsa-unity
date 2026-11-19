using System.Collections;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;
using UnityEngine;
using UnityEngine.TestTools;

namespace Umpire.Unity.Tests {
    public sealed class ShellTests {
        [TearDown]
        public void StopTheApp() => Shells.DestroyAll();

        [Test]
        public void Awake_starts_the_app_and_returns() {
            var shell = Shells.Start();

            Assert.That(shell.IsRunning, Is.True);
            Assert.That(Shell.AppIsRunning, Is.True);
            Assert.That(Shell.Owner, Is.SameAs(shell));
            Assert.That(Shell.Services, Is.Not.Null);
        }

        [Test]
        public void The_Unity_host_adds_the_main_thread_dispatch() {
            Shells.Start();

            Assert.That(Shell.Services.GetService<LivePresenters.Dispatch>(), Is.Not.Null);
        }

        [Test]
        public void The_host_gives_the_UnitySettings_of_the_module_assembly() {
            Shells.Start();

            Assert.That(ProbeModule.Bound.Text, Is.EqualTo("code"));
        }

        [Test]
        public void The_app_hooks_go_to_the_shell() {
            var starts = ProbeShell.Starts;
            var stops = ProbeShell.Stops;
            var shell = Shells.Start();

            shell.Stop();

            Assert.That(ProbeShell.Starts, Is.EqualTo(starts + 1));
            Assert.That(ProbeShell.Stops, Is.EqualTo(stops + 1));
        }

        [Test]
        public void VsaShell_of_an_architecture_uses_the_Unity_host_and_the_hooks() {
            var starts = HookShell.Starts;
            var go = new GameObject("Shell");
            go.AddComponent<HookShell>();

            Assert.That(HookShell.Starts, Is.EqualTo(starts + 1));
            Assert.That(Shell.Services.GetService<LivePresenters.Dispatch>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator A_request_runs_in_the_app() {
            Shells.Start();
            using var scope = Shell.Services.CreateScope();

            var send = scope.ServiceProvider.GetRequiredService<IRequestSender>().SendAsync(new PingRequest("hi"));
            yield return new WaitUntil(() => send.IsCompleted);

            Assert.That(send.Result.Value.Text, Is.EqualTo("pong hi"));
        }

        [Test]
        public void The_app_has_the_lifetime_of_the_Unity_app() {
            var shell = Shells.Start();

            Assert.That(shell.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [Test]
        public void A_shell_on_a_child_object_moves_to_the_root() {
            var parent = new GameObject("Parent");
            var shell = Shells.StartWith(parent.transform);

            Assert.That(shell.transform.parent, Is.Null);
            Assert.That(shell.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void A_second_shell_does_nothing() {
            var first = Shells.Start();
            var services = Shell.Services;

            var second = Shells.Start();

            Assert.That(Shell.Owner, Is.SameAs(first));
            Assert.That(second.enabled, Is.False);
            Assert.That(Shell.Services, Is.SameAs(services));
        }

        [Test]
        public void Stop_stops_the_app_and_Run_starts_it_again() {
            var shell = Shells.Start();
            var first = Shell.Services;

            shell.Stop();
            Assert.That(Shell.AppIsRunning, Is.False);
            Assert.That(Shell.Services, Is.Null);

            shell.Run();
            Assert.That(Shell.AppIsRunning, Is.True);
            Assert.That(Shell.Services, Is.Not.SameAs(first));
        }

        [Test]
        public void The_destroy_of_the_shell_stops_the_app() {
            var shell = Shells.Start();

            Object.DestroyImmediate(shell.gameObject);

            Assert.That(Shell.AppIsRunning, Is.False);
            Assert.That(Shell.Owner, Is.Null);
            Assert.That(Shell.Services, Is.Null);
        }

        [UnityTest]
        public IEnumerator A_presenter_event_from_another_thread_runs_on_the_main_thread() {
            Shells.Start();
            using var scope = Shell.Services.CreateScope();
            var presenter = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var main = System.Threading.Thread.CurrentThread.ManagedThreadId;

            var send = Task.Run(() => presenter.PingAsync("far"));
            yield return new WaitUntil(() => send.IsCompleted && presenter.Last == "far");

            Assert.That(presenter.EventThread, Is.EqualTo(main));
        }
    }

    static class Shells {
        internal static ProbeShell Start(params string[] arguments) => StartWith(null, arguments);

        internal static ProbeShell StartWith(Transform parent, params string[] arguments) {
            var go = new GameObject("Shell");
            go.SetActive(false);
            if (parent != null) {
                go.transform.SetParent(parent);
            }
            var shell = go.AddComponent<ProbeShell>();
            shell.Arguments = arguments;
            go.SetActive(true);
            return shell;
        }

        internal static void DestroyAll() {
            foreach (var shell in Object.FindObjectsByType<Shell>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(shell.gameObject);
            }
        }
    }
}
