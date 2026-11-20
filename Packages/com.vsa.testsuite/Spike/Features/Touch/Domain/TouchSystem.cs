using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed class TouchSystem
    {
        readonly TouchOverlay overlay;

        public TouchSystem(TouchOverlay overlay) => this.overlay = overlay;

        public Task<GameObject> Touch(Point point, CancellationToken cancel) => Hold(point, 0, cancel);

        public async Task<GameObject> Hold(Point point, float seconds, CancellationToken cancel)
        {
            var data = NewData(point);
            var target = Press(data);
            await Awaitable.NextFrameAsync(cancel);
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds)
                await Awaitable.NextFrameAsync(cancel);
            Release(data, target, point);
            if (target != null) ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            await Awaitable.NextFrameAsync(cancel);
            return target;
        }

        public async Task<GameObject> Drag(Point from, Point to, int steps, CancellationToken cancel)
        {
            steps = Math.Max(1, steps);
            var data = NewData(from);
            var target = Press(data);
            var hit = data.pointerCurrentRaycast.gameObject;
            var dragged = hit != null ? ExecuteEvents.GetEventHandler<IDragHandler>(hit) : null;
            if (dragged != null) ExecuteEvents.Execute(dragged, data, ExecuteEvents.initializePotentialDrag);
            data.pointerDrag = dragged;
            await Awaitable.NextFrameAsync(cancel);

            for (var i = 1; i <= steps; i++)
            {
                var point = new Point(
                    Mathf.Lerp(from.X, to.X, i / (float)steps),
                    Mathf.Lerp(from.Y, to.Y, i / (float)steps));
                var position = new Vector2(point.X, point.Y);
                data.delta = position - data.position;
                data.position = position;
                overlay.Moved(point);
                if (dragged != null)
                {
                    if (!data.dragging) ExecuteEvents.Execute(dragged, data, ExecuteEvents.beginDragHandler);
                    data.dragging = true;
                    ExecuteEvents.Execute(dragged, data, ExecuteEvents.dragHandler);
                }
                await Awaitable.NextFrameAsync(cancel);
            }

            var drop = Raycast(data);
            Release(data, target, to);
            if (dragged != null)
            {
                if (drop != null)
                    ExecuteEvents.ExecuteHierarchy(drop.Value.gameObject, data, ExecuteEvents.dropHandler);
                ExecuteEvents.Execute(dragged, data, ExecuteEvents.endDragHandler);
            }
            await Awaitable.NextFrameAsync(cancel);
            return target;
        }

        static PointerEventData NewData(Point point)
        {
            var events = EventSystem.current
                ?? throw new InvalidOperationException("No EventSystem in the scene");
            var position = new Vector2(point.X, point.Y);
            return new PointerEventData(events) { position = position, pressPosition = position };
        }

        static RaycastResult? Raycast(PointerEventData data)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Count > 0 ? hits[0] : (RaycastResult?)null;
        }

        GameObject Press(PointerEventData data)
        {
            overlay.Pressed(new Point(data.position.x, data.position.y));
            var hit = Raycast(data);
            if (hit == null) return null;
            data.pointerPressRaycast = data.pointerCurrentRaycast = hit.Value;
            var target = ExecuteEvents.ExecuteHierarchy(hit.Value.gameObject, data, ExecuteEvents.pointerDownHandler)
                ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.Value.gameObject);
            data.pointerPress = target;
            return target;
        }

        void Release(PointerEventData data, GameObject target, Point point)
        {
            overlay.Released(point);
            if (target != null) ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
        }
    }
}
