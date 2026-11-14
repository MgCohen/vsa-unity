#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VSA.Unity;

namespace VContainerSample.Tests {
    public sealed class VContainerSampleTests {
        const string ScenePath = "Assets/VContainerSample/VContainerSample.unity";

        [UnityTearDown]
        public IEnumerator UnloadTheScene() {
            var empty = SceneManager.CreateScene("Empty " + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            foreach (var scope in Object.FindObjectsByType<SampleRootScope>(FindObjectsInactive.Include)) {
                Object.Destroy(scope.gameObject);
            }
            var sample = SceneManager.GetSceneByPath(ScenePath);
            if (sample.isLoaded) {
                yield return SceneManager.UnloadSceneAsync(sample);
            }
            VContainerSampleApp.Stop();
        }

        [UnityTest]
        public IEnumerator The_button_greets_through_the_Unity_service_and_both_labels_show_it() {
            var single = new LoadSceneParameters(LoadSceneMode.Single);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, single);
            yield return null;

            Assert.That(VsaHost.IsRunning, Is.True);
            var labels = Object.FindObjectsByType<GreetingLabel>();
            Assert.That(labels.Length, Is.EqualTo(2));
            Assert.That(labels[1].Presenter, Is.SameAs(labels[0].Presenter));

            Object.FindAnyObjectByType<GreetButton>().Greet();
            var end = Time.realtimeSinceStartup + 10;
            while (labels[0].Presenter.Count == 0 && Time.realtimeSinceStartup < end) {
                yield return null;
            }
            yield return null;

            var texts = labels.Select(label => label.GetComponent<Text>().text).ToArray();
            Assert.That(texts[0], Does.StartWith("Hello from VContainer, Unity!"));
            Assert.That(texts[1], Is.EqualTo(texts[0]));
        }
    }
}
#endif
