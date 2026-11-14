using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer.Unity;

namespace VContainerSample.Editor {
    public static class VContainerSampleScene {
        const string Path = "Assets/VContainerSample/VContainerSample.unity";

        [MenuItem("Tools/VSA/Write VContainer Sample Scene")]
        public static void Write() {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Root scope").AddComponent<SampleRootScope>();

            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var first = Label(canvas.transform, "Label A", 60);
            var second = Label(canvas.transform, "Label B", 0);
            var button = Button(canvas.transform, "Greet", -80);

            var sceneScope = new GameObject("Scene scope").AddComponent<GreeterSceneScope>();
            sceneScope.parentReference = ParentReference.Create<SampleRootScope>();
            var injected = new List<GameObject> { first.gameObject, second.gameObject, button.gameObject };
            Set(sceneScope, "autoInjectGameObjects", injected);

            var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(sceneScope);
            EditorSceneManager.SaveScene(scene, Path);
            Debug.Log("Wrote " + Path);
        }

        static Text Label(Transform parent, string name, float y) {
            var text = Make<Text>(parent, name, y, new Vector2(600, 50));
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = name;
            var label = text.gameObject.AddComponent<GreetingLabel>();
            Set(label, "label", text);
            return text;
        }

        static GreetButton Button(Transform parent, string caption, float y) {
            var image = Make<Image>(parent, "Greet button", y, new Vector2(200, 50));
            var button = image.gameObject.AddComponent<Button>();
            var text = Make<Text>(image.transform, "Caption", 0, new Vector2(200, 50));
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.text = caption;
            var greet = image.gameObject.AddComponent<GreetButton>();
            Set(greet, "button", button);
            return greet;
        }

        static T Make<T>(Transform parent, string name, float y, Vector2 size) where T : Component {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = size;
            return gameObject.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value) {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string field, List<GameObject> values) {
            var serialized = new SerializedObject(target);
            var list = serialized.FindProperty(field);
            list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
