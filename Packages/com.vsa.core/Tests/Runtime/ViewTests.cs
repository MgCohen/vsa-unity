using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa.Presenters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Umpire.Unity.Tests {
    // A uGUI view of one presenter.
    public sealed class PingLabel : PresenterComponent<PingPresenter> {
        public Text Label;
        public int Renders;

        protected override void Render() {
            Renders++;
            Label.text = Presenter.Last;
        }
    }

    // A view of a controller, not of a presenter.
    public sealed class PingPanel : View<PingController> {
        public int Releases;

        protected override void OnRelease() => Releases++;
    }

    public sealed class ViewTests {
        [TearDown]
        public void StopTheApp() {
            Shells.DestroyAll();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(canvas.gameObject);
            }
            foreach (var panel in Object.FindObjectsByType<PingPanel>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(panel.gameObject);
            }
        }

        [Test]
        public void A_view_in_a_loaded_scene_gets_its_presenter_at_the_app_start() {
            var view = Label();

            Shells.Start();

            Assert.That(view.Presenter, Is.Not.Null);
            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [Test]
        public void A_view_of_a_controller_gets_its_controller_from_the_app_scope() {
            var label = Label();
            var panel = new GameObject("Panel").AddComponent<PingPanel>();

            Shells.Start();

            Assert.That(panel.Target, Is.Not.Null);
            Assert.That(panel.Target.Presenter, Is.SameAs(label.Presenter));
        }

        [Test]
        public void The_app_stop_releases_a_view_of_a_controller() {
            var panel = new GameObject("Panel").AddComponent<PingPanel>();
            var shell = Shells.Start();

            shell.Stop();

            Assert.That(panel.Target, Is.Null);
            Assert.That(panel.Releases, Is.EqualTo(1));
        }

        [Test]
        public void A_view_of_a_controller_that_has_a_target_keeps_it() {
            var panel = new GameObject("Panel").AddComponent<PingPanel>();
            var given = new PingController(null);
            panel.Bind(given);

            Shells.Start();

            Assert.That(panel.Target, Is.SameAs(given));
        }

        [Test]
        public void Two_views_share_one_presenter() {
            var first = Label();
            var second = Label();

            Shells.Start();

            Assert.That(first.Presenter, Is.Not.Null);
            Assert.That(second.Presenter, Is.SameAs(first.Presenter));
        }

        [Test]
        public void An_inactive_view_gets_its_presenter() {
            var view = Label();
            view.gameObject.SetActive(false);

            Shells.Start();

            Assert.That(view.Presenter, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator The_view_renders_the_event_of_its_module() {
            var view = Label();
            Shells.Start();

            var send = view.Presenter.PingAsync("event");
            yield return new WaitUntil(() => send.IsCompleted && view.Label.text == "event");

            Assert.That(view.Label.text, Is.EqualTo("event"));
        }

        [UnityTest]
        public IEnumerator The_presenter_gets_an_event_after_the_destroy_of_the_view() {
            var view = Label();
            Shells.Start();
            var presenter = view.Presenter;
            Object.DestroyImmediate(view.gameObject);

            var send = presenter.PingAsync("alone");
            yield return new WaitUntil(() => send.IsCompleted && presenter.Last == "alone");

            Assert.That(presenter.Disposed, Is.False);
            Assert.That(presenter.Last, Is.EqualTo("alone"));
        }

        [Test]
        public void The_app_stop_disposes_the_presenter_and_releases_the_view() {
            var view = Label();
            var shell = Shells.Start();
            var presenter = view.Presenter;

            shell.Stop();

            Assert.That(presenter.Disposed, Is.True);
            Assert.That(view.Presenter, Is.Null);
        }

        [Test]
        public void A_new_start_gives_the_view_a_new_presenter() {
            var view = Label();
            var shell = Shells.Start();
            var first = view.Presenter;
            shell.Stop();

            shell.Run();

            Assert.That(view.Presenter, Is.Not.Null.And.Not.SameAs(first));
        }

        [Test]
        public void A_view_of_a_factory_keeps_its_presenter_at_the_stop_and_the_start() {
            var shell = Shells.Start();
            using var scope = Shell.Services.CreateScope();
            var given = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();
            view.Bind(given);

            shell.Stop();
            shell.Run();

            Assert.That(view.Presenter, Is.SameAs(given));
        }

        [UnityTest]
        public IEnumerator Bind_gives_the_presenter_of_a_factory() {
            Shells.Start();
            using var scope = Shell.Services.CreateScope();
            var given = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();

            view.Bind(given);
            yield return null;

            Assert.That(view.Presenter, Is.SameAs(given));
            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bind_of_the_same_presenter_changes_nothing() {
            Shells.Start();
            using var scope = Shell.Services.CreateScope();
            var given = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            var view = Label();

            view.Bind(given);
            view.Bind(given);
            yield return null;

            Assert.That(view.Renders, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bind_of_another_presenter_stops_the_last_one() {
            Shells.Start();
            using var scope = Shell.Services.CreateScope();
            var first = scope.ServiceProvider.GetRequiredService<PingPresenter>();
            using var other = Shell.Services.CreateScope();
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
            Shells.Start();
            using var scope = Shell.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PresenterScope>();
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
