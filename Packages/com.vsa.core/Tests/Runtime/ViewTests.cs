using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VSA.Presenters;

namespace VSA.Unity.Tests {
    // A uGUI view of one presenter.
    public sealed class PingLabel : PresenterComponent<PingPresenter> {
        public Text Label;
        public int Renders;

        protected override void Render() {
            Renders++;
            Label.text = Presenter.Last;
        }
    }

    public sealed class ViewTests {
        [TearDown]
        public void StopTheApp() {
            foreach (var host in Object.FindObjectsByType<VsaHost>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(host.gameObject);
            }
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(canvas.gameObject);
            }
            ProbeApp.Stop();
        }

        [UnityTest]
        public IEnumerator A_view_in_a_scene_gets_its_presenter_from_the_app() {
            Host.Start();
            var view = Label();

            yield return null;

            Assert.That(view.Presenter, Is.Not.Null);
            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator The_view_renders_the_event_of_its_module() {
            Host.Start();
            var view = Label();
            yield return null;

            var send = view.Presenter.PingAsync("event");
            yield return new WaitUntil(() => send.IsCompleted && view.Label.text == "event");

            Assert.That(view.Label.text, Is.EqualTo("event"));
        }

        [UnityTest]
        public IEnumerator The_destroy_of_the_view_disposes_its_scope() {
            Host.Start();
            var view = Label();
            yield return null;
            var presenter = view.Presenter;

            Object.DestroyImmediate(view.gameObject);

            Assert.That(presenter.Disposed, Is.True);
        }

        [UnityTest]
        public IEnumerator Bind_before_Start_gives_the_presenter_of_a_factory() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();
            var given = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();

            view.Bind(given);
            yield return null;

            Assert.That(view.Presenter, Is.SameAs(given));
            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bind_of_the_same_presenter_changes_nothing() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();
            var given = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();

            view.Bind(given);
            view.Bind(given);
            yield return null;

            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bind_of_another_presenter_stops_the_last_one() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();
            var first = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            using var other = VsaHost.Services.CreateScope();
            var second = other.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();
            view.Bind(first);
            view.Bind(second);
            var renders = view.Renders;

            var touch = first.TouchAsync();
            yield return new WaitUntil(() => touch.IsCompleted);
            yield return null;

            Assert.That(view.Renders, Is.EqualTo(renders));
        }

        [UnityTest]
        public IEnumerator A_destroyed_view_does_not_render() {
            Host.Start();
            using var scope = VsaHost.Services.CreateScope();
            var presenter = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();
            view.Bind(presenter);
            var renders = view.Renders;

            Object.DestroyImmediate(view.gameObject);
            var send = presenter.PingAsync("late");
            yield return new WaitUntil(() => send.IsCompleted && presenter.Last == "late");
            yield return null;

            Assert.That(view.Renders, Is.EqualTo(renders));
        }

        static PingLabel Label() {
            var canvas = new GameObject("Canvas", typeof(Canvas));
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(canvas.transform);
            var label = go.AddComponent<Text>();
            var view = go.AddComponent<PingLabel>();
            view.Label = label;
            return view;
        }
    }
}
