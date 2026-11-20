using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VSA.TestSuite.Spike.Features.Touch;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed class ObjectSystem
    {
        Scene keptScene;

        public GameObject Find(string path)
            => GameObject.Find(path) ?? throw new InvalidOperationException($"No active object at '{path}'");

        public ObjectState State(string path)
        {
            var target = Walk(path);
            return target == null ? ObjectState.Missing
                : target.activeInHierarchy ? ObjectState.Active
                : ObjectState.Inactive;
        }

        public string Text(string path)
        {
            var target = Find(path);
            foreach (var component in target.GetComponents<Component>())
            {
                var property = component == null ? null : component.GetType().GetProperty("text", typeof(string));
                if (property != null && property.CanRead) return (string)property.GetValue(component);
            }
            throw new InvalidOperationException($"'{path}' has no component with a text");
        }

        public Point ScreenPoint(GameObject target)
        {
            if (target.transform is RectTransform rect)
            {
                var canvas = rect.GetComponentInParent<Canvas>();
                var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;
                var screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                return new Point(screen.x, screen.y);
            }
            var main = Camera.main
                ?? throw new InvalidOperationException("No main camera to project a world object");
            var world = main.WorldToScreenPoint(target.transform.position);
            return new Point(world.x, world.y);
        }

        GameObject Walk(string path)
        {
            var parts = path.Trim('/').Split('/');
            foreach (var root in Roots())
            {
                if (root.name != parts[0]) continue;
                var current = root.transform;
                for (var i = 1; current != null && i < parts.Length; i++)
                    current = current.Find(parts[i]);
                if (current != null) return current.gameObject;
            }
            return null;
        }

        IEnumerable<GameObject> Roots()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    foreach (var root in scene.GetRootGameObjects()) yield return root;
            }
            var kept = KeptScene();
            if (kept.isLoaded)
                foreach (var root in kept.GetRootGameObjects()) yield return root;
        }

        Scene KeptScene()
        {
            if (keptScene.IsValid() && keptScene.isLoaded) return keptScene;
            var probe = new GameObject("VSA.SceneProbe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            keptScene = probe.scene;
            UnityEngine.Object.DestroyImmediate(probe);
            return keptScene;
        }
    }
}
