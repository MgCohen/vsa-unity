using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite
{
    /// <summary>A found object and the screen point to click it.</summary>
    public sealed class ObjectRef
    {
        public string Path;
        public float[] ScreenPoint;
        [JsonIgnore] public GameObject GameObject;
    }

    /// <summary>Finds objects by hierarchy path in all loaded scenes, active or not.</summary>
    /// <summary>The state of an object, as a wait or an assert sees it.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ObjectState { Missing, Inactive, Active }

    public sealed class ObjectSystem
    {
        readonly WaitSystem wait;

        public ObjectSystem(WaitSystem wait) => this.wait = wait;

        /// <summary>Returns Missing, Inactive or Active. Never fails.</summary>
        public ObjectState State(string path)
        {
            var go = Resolve(path, out _);
            return go == null ? ObjectState.Missing : go.activeInHierarchy ? ObjectState.Active : ObjectState.Inactive;
        }

        public Task<WaitResult> WaitFor(string path, ObjectState state, int timeoutMs, CancellationToken cancel)
            => wait.Until(() => State(path) == state, timeoutMs, "'" + path + "' to be " + state, cancel);

        public ObjectState AssertState(string path, ObjectState expected)
        {
            var actual = State(path);
            if (actual != expected)
                throw new ActionFailed(FailCode.AssertFailed, "'" + path + "' is " + actual + ", expected " + expected,
                    new { expected = expected.ToString(), actual = actual.ToString() });
            return actual;
        }

        /// <summary>Reads the text of a uGUI Text, a TextMeshPro text, or any component with a string "text" property.</summary>
        public string ReadText(string path)
        {
            var go = Find(path).GameObject;
            foreach (var c in go.GetComponents<Component>())
            {
                var prop = c == null ? null : c.GetType().GetProperty("text", typeof(string));
                if (prop != null && prop.CanRead) return (string)prop.GetValue(c);
            }
            throw new ActionFailed(FailCode.NotFound, "'" + path + "' has no component with a text");
        }

        public string AssertText(string path, string expected)
        {
            var actual = ReadText(path);
            if (actual != expected)
                throw new ActionFailed(FailCode.AssertFailed, "'" + path + "' text is '" + actual + "', expected '" + expected + "'",
                    new { expected, actual });
            return actual;
        }

        static GameObject Resolve(string path, out string error)
        {
            error = null;
            var parts = path.Trim('/').Split('/');
            var current = AllRoots().FirstOrDefault(r => r.name == parts[0]);
            for (int i = 1; current != null && i < parts.Length; i++)
            {
                var next = current.transform.Find(parts[i]);
                current = next == null ? null : next.gameObject;
            }
            return current;
        }

        public ObjectRef Find(string path)
        {
            var parts = path.Trim('/').Split('/');
            var roots = AllRoots().ToList();
            var current = roots.FirstOrDefault(r => r.name == parts[0]);
            if (current == null)
                throw new ActionFailed(FailCode.NotFound, "No root object '" + parts[0] + "'",
                    new { roots = roots.Select(r => r.name).ToArray() });

            for (int i = 1; i < parts.Length; i++)
            {
                var next = current.transform.Find(parts[i]);
                if (next == null)
                    throw new ActionFailed(FailCode.NotFound, "No child '" + parts[i] + "' under '" + string.Join("/", parts.Take(i)) + "'",
                        new { children = Children(current).ToArray() });
                current = next.gameObject;
            }

            if (!current.activeInHierarchy)
                throw new ActionFailed(FailCode.NotActive, "Object '" + path + "' is not active");

            return new ObjectRef { Path = path, GameObject = current, ScreenPoint = ScreenPointOf(current) };
        }

        static IEnumerable<GameObject> AllRoots()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    foreach (var go in scene.GetRootGameObjects()) yield return go;
            }
            // Many games keep their UI in DontDestroyOnLoad. SceneManager does not list that scene.
            var ddol = DontDestroyScene();
            if (ddol.IsValid() && ddol.isLoaded)
                foreach (var go in ddol.GetRootGameObjects()) yield return go;
        }

        static Scene ddolScene;

        // The only way to get the DontDestroyOnLoad scene is from an object in it.
        static Scene DontDestroyScene()
        {
            if (!Application.isPlaying) return default;
            if (ddolScene.IsValid() && ddolScene.isLoaded) return ddolScene;
            var probe = new GameObject("VSA.SceneProbe");
            Object.DontDestroyOnLoad(probe);
            ddolScene = probe.scene;
            Object.DestroyImmediate(probe);
            return ddolScene;
        }

        static IEnumerable<string> Children(GameObject go)
        {
            foreach (Transform t in go.transform) yield return t.name;
        }

        static float[] ScreenPointOf(GameObject go)
        {
            Vector2 p;
            if (go.transform is RectTransform rect)
            {
                var canvas = go.GetComponentInParent<Canvas>();
                var cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                p = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            }
            else
            {
                var r = go.GetComponentInChildren<Renderer>();
                var world = r != null ? r.bounds.center : go.transform.position;
                var cam = Camera.main;
                if (cam == null) throw new ActionFailed(FailCode.Failed, "No main camera to find the screen point of '" + go.name + "'");
                p = cam.WorldToScreenPoint(world);
            }
            return new[] { Mathf.Round(p.x), Mathf.Round(p.y) };
        }
    }
}
