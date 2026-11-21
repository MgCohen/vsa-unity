using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Umpire.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DiceGame.PlayTests {
    // The Play Mode check of the dice sample: menu, Start, two rolls. It uses the scenes and the object names only,
    // because a test assembly cannot reference the app code in Assembly-CSharp.
    public sealed class DiceGameTests {
        const float WaitSeconds = 10;

        // Stops the app and unloads the dice scenes, so the next tests do not see their views.
        [UnityTearDown]
        public IEnumerator StopTheApp() {
            foreach (var shell in Object.FindObjectsByType<Shell>(FindObjectsInactive.Include)) {
                Object.DestroyImmediate(shell.gameObject);
            }
            var empty = SceneManager.CreateScene("Empty after dice");
            SceneManager.SetActiveScene(empty);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty) {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [UnityTest]
        public IEnumerator Two_rolls_show_two_bound_roll_entries() {
            SceneManager.LoadScene("MainMenu");
            yield return null;

            Button("Start Button").onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "DiceGame");
            var label = GameObject.Find("Count Label").GetComponent<Text>();
            var list = GameObject.Find("Roll List").transform;
            yield return Until(() => label.text.StartsWith("Rolls: ", StringComparison.Ordinal));
            var before = Rolls(label);

            Button("Roll Button").onClick.Invoke();
            yield return Until(() => Rolls(label) == before + 1 && list.childCount == 1);
            Button("Roll Button").onClick.Invoke();
            yield return Until(() => Rolls(label) == before + 2 && list.childCount == 2);

            Assert.That(list.childCount, Is.EqualTo(2), label.text);
            var entries = Enumerable.Range(0, 2).Select(i => list.GetChild(i).GetComponent<Text>().text).ToArray();
            Assert.That(entries[0], Does.StartWith("Roll " + (before + 1) + ": "));
            Assert.That(entries[1], Is.EqualTo("Roll " + (before + 2) + ": " + Last(label)));
            for (var i = 0; i < list.childCount; i++) {
                var view = list.GetChild(i).GetComponents<MonoBehaviour>().Single(b => b.GetType().Name == "RollEntryView");
                Assert.That(view.GetType().GetProperty("Target").GetValue(view), Is.Not.Null, view.name);
            }
        }

        static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();

        // "Rolls: 2   Last: 5"
        static int Rolls(Text label) => int.Parse(label.text.Split(' ')[1]);

        static string Last(Text label) => label.text.Split(' ').Last();

        static IEnumerator Until(Func<bool> done) {
            var end = Time.realtimeSinceStartup + WaitSeconds;
            while (!done() && Time.realtimeSinceStartup < end) {
                yield return null;
            }
        }
    }
}
