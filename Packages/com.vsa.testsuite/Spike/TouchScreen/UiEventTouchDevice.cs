using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VSA.TestSuite.Spike
{
    /// <summary>Sends uGUI pointer events to the top UI object. It needs no input package.</summary>
    public sealed class UiEventTouchDevice : ITouchDevice
    {
        PointerEventData data;
        GameObject pressed;

        public void Move(Vector2 point) { }

        public void Press(Vector2 point, bool down)
        {
            var events = EventSystem.current
                ?? throw new InvalidOperationException("No EventSystem in the scene");
            if (down)
            {
                data = new PointerEventData(events) { position = point, pressPosition = point };
                var hits = new List<RaycastResult>();
                events.RaycastAll(data, hits);
                if (hits.Count == 0) { pressed = null; return; }
                data.pointerPressRaycast = data.pointerCurrentRaycast = hits[0];
                pressed = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler)
                    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                data.pointerPress = pressed;
                return;
            }
            if (data == null || pressed == null) return;
            ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerClickHandler);
            pressed = null;
            data = null;
        }
    }
}
