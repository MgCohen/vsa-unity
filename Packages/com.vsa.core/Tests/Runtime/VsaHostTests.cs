using System.Collections;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VSA.Presenters;

namespace VSA.Unity.Tests {
    public sealed class VsaHostTests {
        [TearDown]
        public void StopTheApp() {
            foreach (var host in Object.FindObjectsByType<VsaHost>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(host.gameObject);
            }
            ProbeApp.Stop();
        }

        [Test]
        public void Awake_runs_the_app_and_returns() {
            var host = Host.Start();

            Assert.That(VsaHost.IsRunning, Is.True);
            Assert.That(ProbeApp.IsRunning, Is.True);
            Assert.That(host.AppType, Is.EqualTo(typeof(ProbeApp)));
            Assert.That(VsaHost.Owner, Is.SameAs(host));
        }

        [Test]
        public void UseUnityHost_selects_the_Unity_host() {
            Host.Start();

            Assert.That(VsaHost.Services.GetRequiredService<IHostLifetime>().GetType().Name, Is.EqualTo("UnityLifetime"));
            Assert.That(VsaHost.Services.GetService<LivePresenters.Dispatch>(), Is.Not.Null);
        }

        [Test]
        public void The_arguments_go_to_the_configuration() {
            Host.Start("--Probe:Value=42");

            Assert.That(VsaHost.Services.GetRequiredService<IConfiguration>()["Probe:Value"], Is.EqualTo("42"));
        }

        [UnityTest]
        public IEnumerator A_request_runs_in_the_app() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();

            var send = scope.ServiceProvider.GetRequiredService<IBus>().Send(new PingRequest("hi"));
            yield return new WaitUntil(() => send.IsCompleted);

            Assert.That(send.Result.Value.Text, Is.EqualTo("pong hi"));
        }

        [Test]
        public void The_app_has_the_lifetime_of_the_Unity_app() {
            var host = Host.Start();

            Assert.That(host.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [Test]
        public void A_host_on_a_child_object_moves_to_the_root() {
            var parent = new GameObject("Parent");
            var host = Host.StartWith(parent.transform);

            Assert.That(host.transform.parent, Is.Null);
            Assert.That(host.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void A_second_host_does_nothing() {
            var first = Host.Start();
            var services = VsaHost.Services;

            var second = Host.Start();

            Assert.That(VsaHost.Owner, Is.SameAs(first));
            Assert.That(second.enabled, Is.False);
            Assert.That(VsaHost.Services, Is.SameAs(services));
        }

        [Test]
        public void Stop_stops_the_app_and_Run_starts_it_again() {
            var host = Host.Start();
            var first = VsaHost.Services;

            host.Stop();
            Assert.That(VsaHost.IsRunning, Is.False);
            Assert.That(ProbeApp.IsRunning, Is.False);

            host.Run();
            Assert.That(VsaHost.IsRunning, Is.True);
            Assert.That(VsaHost.Services, Is.Not.SameAs(first));
        }

        [Test]
        public void The_destroy_of_the_host_stops_the_app() {
            var host = Host.Start();

            Object.DestroyImmediate(host.gameObject);

            Assert.That(VsaHost.IsRunning, Is.False);
            Assert.That(VsaHost.Owner, Is.Null);
        }

        [UnityTest]
        public IEnumerator A_presenter_event_from_another_thread_runs_on_the_main_thread() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();
            var presenter = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var main = System.Threading.Thread.CurrentThread.ManagedThreadId;

            var send = Task.Run(() => presenter.PingAsync("far"));
            yield return new WaitUntil(() => send.IsCompleted && presenter.Last == "far");

            Assert.That(presenter.EventThread, Is.EqualTo(main));
        }
    }

    static class Host {
        internal static VsaHost Start(params string[] arguments) => StartWith(null, arguments);

        internal static VsaHost StartWith(Transform parent, params string[] arguments) {
            var go = new GameObject("VsaHost");
            go.SetActive(false);
            if (parent != null) {
                go.transform.SetParent(parent);
            }
            var host = go.AddComponent<ProbeHost>();
            host.Arguments = arguments;
            go.SetActive(true);
            return host;
        }
    }
}
