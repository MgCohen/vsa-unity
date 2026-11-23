using System;
using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umpire.Factories;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Umpire.Unity.Factories.Tests {
    public sealed class CharacterModel {
        public int Health { get; set; } = 10;
    }

    public sealed class ItemData {
        public string Name;
    }

    public sealed class ItemModel {
        public ItemModel(ItemData data) => Data = data;

        public ItemData Data { get; }
    }

    public sealed class DisposableModel : IDisposable {
        public void Dispose() {
        }
    }

    // A behaviour that is not a view. It records what it had in Start, and each release.
    public sealed class Character : Bound<CharacterModel> {
        public CharacterModel TargetInStart;
        public bool Started;
        public int Binds;
        public int Releases;

        protected override void OnBind() => Binds++;

        protected override void OnRelease() => Releases++;

        void Start() {
            Started = true;
            TargetInStart = Target;
        }
    }

    public sealed class Item : Bound<ItemModel> {
    }

    public sealed class Holder : Bound<DisposableModel> {
    }

    public sealed class Plain : MonoBehaviour {
    }

    public sealed class PrefabFactoryTests {
        GameObject prefabs;

        [SetUp]
        public void MakePrefabRoot() {
            prefabs = new GameObject("Prefabs");
            prefabs.SetActive(false);
        }

        [TearDown]
        public void DestroyAll() {
            // Only the objects of these tests: the prefab root, and each root that holds a behaviour of this namespace.
            Object.DestroyImmediate(prefabs);
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)) {
                if (behaviour != null && behaviour.GetType().Namespace == typeof(PrefabFactoryTests).Namespace) {
                    Object.DestroyImmediate(behaviour.transform.root.gameObject);
                }
            }
            var parent = GameObject.Find("Parent");
            if (parent != null) {
                Object.DestroyImmediate(parent);
            }
        }

        // A prefab under an inactive root, so it does not run Awake or Start. Its copies are active.
        T Prefab<T>(string name) where T : Component {
            var go = new GameObject(name);
            go.transform.SetParent(prefabs.transform, false);
            return go.AddComponent<T>();
        }

        static ServiceProvider Build(Action<IServiceCollection> configure) {
            var services = new ServiceCollection();
            configure(services);
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [UnityTest]
        public IEnumerator A_copy_has_its_target_before_Start() {
            using var provider = Build(services => services.AddPrefabFactory<Character>());
            var prefab = Prefab<Character>("Character");

            var copy = provider.GetRequiredService<IPrefabFactory<Character>>().Create(prefab);
            yield return null;

            Assert.That(copy, Is.Not.SameAs(prefab));
            Assert.That(copy.Started, Is.True);
            Assert.That(copy.TargetInStart, Is.Not.Null);
            Assert.That(copy.Binds, Is.EqualTo(1));
        }

        [Test]
        public void AddPrefabFactory_registers_the_target_as_transient() {
            using var provider = Build(services => services.AddPrefabFactory<Character>());
            var prefab = Prefab<Character>("Character");
            var factory = provider.GetRequiredService<IPrefabFactory<Character>>();

            Assert.That(factory.Create(prefab).Target, Is.Not.SameAs(factory.Create(prefab).Target));
        }

        [Test]
        public void AddPrefabFactory_keeps_a_scoped_target() {
            using var provider = Build(services => {
                services.AddScoped<CharacterModel>();
                services.AddPrefabFactory<Character>();
            });
            using var scope = provider.CreateScope();
            var prefab = Prefab<Character>("Character");
            var factory = scope.ServiceProvider.GetRequiredService<IPrefabFactory<Character>>();

            Assert.That(factory.Create(prefab).Target, Is.SameAs(factory.Create(prefab).Target));
        }

        [Test]
        public void A_target_with_a_factory_comes_from_that_factory() {
            using var provider = Build(services => {
                services.AddFactory(p => new CharacterModel { Health = 3 });
                services.AddPrefabFactory<Character>();
            });
            var prefab = Prefab<Character>("Character");

            Assert.That(provider.GetRequiredService<IPrefabFactory<Character>>().Create(prefab).Target.Health, Is.EqualTo(3));
        }

        [Test]
        public void Child_and_inactive_behaviours_of_the_copy_bind() {
            using var provider = Build(services => services.AddPrefabFactory<Plain>().AddFactory<CharacterModel>());
            var prefab = Prefab<Plain>("Group");
            var child = new GameObject("Child").AddComponent<Character>();
            child.transform.SetParent(prefab.transform, false);
            var hidden = new GameObject("Hidden").AddComponent<Character>();
            hidden.transform.SetParent(prefab.transform, false);
            hidden.gameObject.SetActive(false);

            var copy = provider.GetRequiredService<IPrefabFactory<Plain>>().Create(prefab);

            foreach (var character in copy.GetComponentsInChildren<Character>(true)) {
                Assert.That(character.Target, Is.Not.Null, character.name);
            }
            Assert.That(copy.GetComponentsInChildren<Character>(true).Length, Is.EqualTo(2));
        }

        [Test]
        public void A_prefab_with_no_bindable_behaviour_gives_a_copy() {
            using var provider = Build(services => services.AddPrefabFactory<Plain>());
            var prefab = Prefab<Plain>("Plain");

            Assert.That(provider.GetRequiredService<IPrefabFactory<Plain>>().Create(prefab), Is.Not.Null);
        }

        [Test]
        public void The_copy_goes_under_the_parent() {
            using var provider = Build(services => services.AddPrefabFactory<Character>());
            var parent = new GameObject("Parent").transform;

            var copy = provider.GetRequiredService<IPrefabFactory<Character>>().Create(Prefab<Character>("Character"), parent);

            Assert.That(copy.transform.parent, Is.SameAs(parent));
        }

        [UnityTest]
        public IEnumerator Destroy_releases_the_target() {
            using var provider = Build(services => services.AddPrefabFactory<Character>());
            var copy = provider.GetRequiredService<IPrefabFactory<Character>>().Create(Prefab<Character>("Character"));

            Object.Destroy(copy.gameObject);
            yield return null;

            Assert.That(copy.Releases, Is.EqualTo(1));
            Assert.That(copy.Target, Is.Null);
        }

        [UnityTest]
        public IEnumerator A_hundred_copies_bind_and_release() {
            using var provider = Build(services => services.AddPrefabFactory<Character>());
            var factory = provider.GetRequiredService<IPrefabFactory<Character>>();
            var prefab = Prefab<Character>("Character");
            var releases = 0;

            for (var i = 0; i < 100; i++) {
                var copy = factory.Create(prefab);
                Object.DestroyImmediate(copy.gameObject);
                releases += copy.Releases;
            }
            yield return null;

            Assert.That(releases, Is.EqualTo(100));
        }

        [Test]
        public void A_factory_with_an_argument_binds_the_copy_with_the_argument() {
            using var provider = Build(services => {
                services.AddFactory<ItemData, ItemModel>((p, data) => new ItemModel(data));
                services.AddPrefabFactory<ItemData, Item>();
            });
            var data = new ItemData { Name = "Sword" };

            var copy = provider.GetRequiredService<IPrefabFactory<ItemData, Item>>().Create(Prefab<Item>("Item"), data);

            Assert.That(copy.Target.Data, Is.SameAs(data));
        }

        [Test]
        public void A_factory_with_an_argument_and_no_target_factory_gives_a_clear_error() {
            using var provider = Build(services => services.AddPrefabFactory<ItemData, Item>());
            var factory = provider.GetRequiredService<IPrefabFactory<ItemData, Item>>();
            var prefab = Prefab<Item>("Item");

            var error = Assert.Throws<InvalidOperationException>(() => factory.Create(prefab, new ItemData()));

            StringAssert.Contains("AddFactory<TArg, ItemModel>", error.Message);
        }

        [Test]
        public void A_disposable_target_that_AddPrefabFactory_registers_stops_the_start() {
            Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPrefabFactory<Holder>());
        }

        [Test]
        public void Bind_of_another_target_releases_the_last_one() {
            var character = new GameObject("Character").AddComponent<Character>();

            character.Bind(new CharacterModel());
            character.Bind(new CharacterModel());

            Assert.That(character.Binds, Is.EqualTo(2));
            Assert.That(character.Releases, Is.EqualTo(1));
        }
    }
}
