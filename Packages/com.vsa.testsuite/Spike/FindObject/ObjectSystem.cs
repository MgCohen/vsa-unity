using System;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Finds objects and gives their screen points. Simple on purpose: GameObject.Find sees
    /// active objects only, and it searches all loaded scenes each time.
    /// </summary>
    public sealed class ObjectSystem
    {
        public GameObject Find(string path)
        {
            var target = GameObject.Find(path)
                ?? throw new InvalidOperationException($"No active object at '{path}'");
            Debug.Log($"{LogHook.Tag} found {path}");
            return target;
        }

        /// <summary>The center of a UI object, or the screen projection of a world object.</summary>
        public Vector2 ScreenPoint(GameObject target)
        {
            if (target.transform is RectTransform rect)
            {
                var canvas = rect.GetComponentInParent<Canvas>();
                var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;
                return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            }
            var main = Camera.main
                ?? throw new InvalidOperationException("No main camera to project a world object");
            return main.WorldToScreenPoint(target.transform.position);
        }
    }
}
