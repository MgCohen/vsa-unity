using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Touches a screen point with uGUI pointer events: press, then release and click,
    /// with one frame between each step. It reaches UI objects only.
    /// </summary>
    public sealed class TouchSystem
    {
        public async Task Touch(Point point, CancellationToken cancel)
        {
            var position = new Vector2(point.X, point.Y);
            var events = EventSystem.current
                ?? throw new InvalidOperationException("No EventSystem in the scene");
            var data = new PointerEventData(events) { position = position, pressPosition = position };

            var target = Press(events, data);
            await Awaitable.NextFrameAsync(cancel);
            Release(data, target);
            await Awaitable.NextFrameAsync(cancel);
        }

        static GameObject Press(EventSystem events, PointerEventData data)
        {
            var hits = new List<RaycastResult>();
            events.RaycastAll(data, hits);
            if (hits.Count == 0)
            {
                Debug.Log($"{LogHook.Tag} touch on nothing");
                return null;
            }
            data.pointerPressRaycast = data.pointerCurrentRaycast = hits[0];
            var target = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler)
                ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            data.pointerPress = target;
            Debug.Log($"{LogHook.Tag} touch on {(target != null ? target.name : "nothing")}");
            return target;
        }

        static void Release(PointerEventData data, GameObject target)
        {
            if (target == null) return;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }
    }
}
