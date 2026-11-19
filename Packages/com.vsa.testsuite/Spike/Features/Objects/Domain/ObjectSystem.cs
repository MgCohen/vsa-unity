using System;
using UnityEngine;
using VSA.TestSuite.Spike.Features.Touch;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed class ObjectSystem
    {
        public GameObject Find(string path)
            => GameObject.Find(path) ?? throw new InvalidOperationException($"No active object at '{path}'");

        public GameObject TryFind(string path) => GameObject.Find(path);

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
    }
}
