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
using Umpire.Vsa.Contracts.Requests;
using Object = UnityEngine.Object;

namespace Umpire.Unity.VContainer.Tests {
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
        }

        [Test]
        public void The_root_scope_starts_the_app() {
            var root = Root();

            Assert.That(root.AppIsRunning, Is.True);
            Assert.That(root.StartedTheApp, Is.True);
            Assert.That(Shell.Services, Is.Not.Null);
        }

        [Test]
        public void Destroy_of_the_root_scope_stops_the_app() {
            var root = Root();

            Object.DestroyImmediate(root.gameObject);

            Assert.That(Shell.AppIsRunning, Is.False);
            Assert.That(Shell.Services, Is.Null);
        }

        [Test]
        public void The_app_gets_the_imports_as_the_same_instance() {
            var root = Root();

            var inApp = Shell.Services.GetRequiredService<IGreeting>();

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
            var child = Child(root, builder => builder.RegisterVsa<IRequestSender>());

            Assert.That(child.Container.Resolve<IRequestSender>(), Is.Not.Null);
        }

        [Test]
        public void Destroy_of_a_scope_disposes_its_presenters() {
            var child = Child(Root());
            var presenter = child.Container.Resolve<GreetPresenter>();

            Object.DestroyImmediate(child.gameObject);

            Assert.That(presenter.Disposed, Is.True);
            Assert.That(Shell.AppIsRunning, Is.True);
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
        public void Imports_with_a_host_that_is_not_UnityHost_fail_and_stop_the_app() {
            LogAssert.Expect(LogType.Exception, new Regex("does not derive from UnityHost"));

            var scope = Made(new GameObject("Root")).AddComponent<PlainScope>();

            Assert.That(scope.AppIsRunning, Is.False);
            Assert.That(Shell.AppIsRunning, Is.False);
        }

        [Test]
        public void A_root_scope_fails_if_a_shell_runs_an_app() {
            var shell = Made(new GameObject("Shell")).AddComponent<BridgeShell>();
            LogAssert.Expect(LogType.Exception, new Regex("an Umpire app runs already"));

            var scope = Made(new GameObject("Root")).AddComponent<BridgeScope>();

            Assert.That(scope.StartedTheApp, Is.False);
            Assert.That(shell.IsRunning, Is.True);
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
