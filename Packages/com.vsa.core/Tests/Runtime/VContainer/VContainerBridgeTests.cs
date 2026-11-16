using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;
using VSA.Presenters;
using Object = UnityEngine.Object;

namespace VSA.Unity.VContainer.Tests {
    public sealed class VContainerBridgeTests {
        readonly List<GameObject> made = new List<GameObject>();

        [TearDown]
        public void DestroyTheScopes() {
            // Children first: a child scope disposes before its parent.
            for (var i = made.Count - 1; i >= 0; i--) {
                if (made[i] != null) {
                    Object.DestroyImmediate(made[i]);
                }
            }
            made.Clear();
            BridgeApp.Stop();
            NoBridgeApp.Stop();
        }

        [Test]
        public void The_root_scope_starts_the_app() {
            var root = Root();

            Assert.That(BridgeApp.IsRunning, Is.True);
            Assert.That(root.StartedTheApp, Is.True);
            Assert.That(VsaHost.Services, Is.Not.Null);
        }

        [Test]
        public void Destroy_of_the_root_scope_stops_the_app() {
            var root = Root();

            Object.DestroyImmediate(root.gameObject);

            Assert.That(BridgeApp.IsRunning, Is.False);
            Assert.That(VsaHost.IsRunning, Is.False);
        }

        [Test]
        public void The_app_gets_the_imports_as_the_same_instance() {
            var root = Root();

            var inApp = VsaHost.Services.GetRequiredService<IGreeting>();

            Assert.That(inApp, Is.SameAs(root.Container.Resolve<IGreeting>()));
        }

        [UnityTest]
        public IEnumerator A_VSA_handler_uses_an_import() {
            var root = Root();
            var presenter = root.Container.Resolve<GreetPresenter>();

            var send = presenter.GreetAsync("ann");
            yield return new WaitUntil(() => send.IsCompleted && presenter.Last != "");

            Assert.That(send.Result.Value.Text, Is.EqualTo("hello ann"));
            Assert.That(presenter.Last, Is.EqualTo("hello ann"));
        }

        [Test]
        public void One_scope_shares_one_presenter() {
            var child = Child(Root());

            var first = child.Container.Resolve<GreetPresenter>();
            var second = child.Container.Resolve<GreetPresenter>();

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Two_scopes_get_two_presenters() {
            var root = Root();

            var first = Child(root).Container.Resolve<GreetPresenter>();
            var second = Child(root).Container.Resolve<GreetPresenter>();

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void A_child_scope_can_register_more_VSA_services() {
            var root = Root();
            var child = Child(root, builder => builder.RegisterVsa<IBus>());

            Assert.That(child.Container.Resolve<IBus>(), Is.Not.Null);
        }

        [Test]
        public void Destroy_of_a_scope_disposes_its_presenters() {
            var child = Child(Root());
            var presenter = child.Container.Resolve<GreetPresenter>();

            Object.DestroyImmediate(child.gameObject);

            Assert.That(presenter.Disposed, Is.True);
            Assert.That(BridgeApp.IsRunning, Is.True);
        }

        [Test]
        public void RegisterVsaPresenters_registers_the_presenters_of_the_modules() {
            var child = Child(Root(), builder => builder.RegisterVsaPresenters(new BridgeModule()));

            Assert.That(child.Container.Resolve<GreetPresenter>(), Is.Not.Null);
        }

        [Test]
        public void The_root_scope_injects_its_objects_after_the_app_starts() {
            var view = Made(new GameObject("View")).AddComponent<GreetView>();
            BridgeScope.NextTargets = new List<GameObject> { view.gameObject };

            Root();

            Assert.That(view.Presenter, Is.Not.Null);
            Assert.That(view.AppRanAtInject, Is.True);
        }

        [Test]
        public void An_app_without_UseVContainer_fails_and_stops() {
            LogAssert.Expect(LogType.Exception, new Regex("does not call UseVContainer"));

            Made(new GameObject("Root")).AddComponent<NoBridgeScope>();

            Assert.That(NoBridgeApp.IsRunning, Is.False);
        }

        [Test]
        public void A_scope_fails_if_no_app_runs() {
            var scope = Made(new GameObject("Scope")).AddComponent<LifetimeScope>();
            var child = scope.CreateChild(builder => builder.RegisterVsa<GreetPresenter>());
            made.Add(child.gameObject);

            var error = Assert.Catch(() => child.Container.Resolve<GreetPresenter>());

            Assert.That(error.ToString(), Does.Contain("No VSA app runs"));
        }

        BridgeScope Root() => Made(new GameObject("Root")).AddComponent<BridgeScope>();

        LifetimeScope Child(LifetimeScope parent, Action<IContainerBuilder> configure = null) {
            var child = parent.CreateChild(configure ?? (_ => { }));
            made.Add(child.gameObject);
            return child;
        }

        GameObject Made(GameObject gameObject) {
            made.Add(gameObject);
            return gameObject;
        }
    }
}
