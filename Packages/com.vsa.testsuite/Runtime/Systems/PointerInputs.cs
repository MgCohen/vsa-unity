using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if VSA_TESTSUITE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace VSA.TestSuite
{
#if VSA_TESTSUITE_INPUT_SYSTEM
    /// <summary>
    /// Queues mouse state events. It does not call InputSystem.Update(): in Editor Play Mode
    /// a manual update loses the event, so the player loop must process it.
    /// </summary>
    public sealed class InputSystemPointer : IPointerInput
    {
        public string Name => "input-system-queue";

        public void Move(Vector2 p) => Queue(p, null, false);

        public void Press(Vector2 p, bool down) => Queue(p, CurrentMouse().leftButton, down);

        // A batch mode Editor or a touch device has no mouse. Then add one virtual mouse, so the
        // clicks still go through the Input System and its UI module.
        static Mouse CurrentMouse()
        {
            if (Mouse.current != null) return Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>("VSA TestSuite Mouse");
            mouse.MakeCurrent();
            return mouse;
        }

        static void Queue(Vector2 p, ButtonControl button, bool pressed)
        {
            var mouse = CurrentMouse();
            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(p, eventPtr);
                if (button != null) button.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
#endif

    /// <summary>
    /// Sends UI pointer events straight to the top UI object. It needs no input package,
    /// but it reaches only UI objects, not game code that reads input devices.
    /// </summary>
    public sealed class UiEventPointer : IPointerInput
    {
        GameObject pressed;
        GameObject dragged;
        PointerEventData data;

        public string Name => "ui-events";

        public void Move(Vector2 p)
        {
            if (data == null) return;
            data.delta = p - data.position;
            data.position = p;
            if (dragged == null && data.pointerPressRaycast.gameObject != null)
            {
                dragged = ExecuteEvents.GetEventHandler<IDragHandler>(data.pointerPressRaycast.gameObject);
                if (dragged != null)
                {
                    data.dragging = true;
                    data.pointerDrag = dragged;
                    ExecuteEvents.Execute(dragged, data, ExecuteEvents.beginDragHandler);
                }
            }
            if (dragged != null) ExecuteEvents.Execute(dragged, data, ExecuteEvents.dragHandler);
        }

        public void Press(Vector2 p, bool down)
        {
            var es = EventSystem.current
                ?? throw new ActionFailed(FailCode.NoDevice, "No EventSystem in the scene for UI events");
            if (down)
            {
                data = new PointerEventData(es) { position = p, pressPosition = p, button = PointerEventData.InputButton.Left };
                var results = new List<RaycastResult>();
                es.RaycastAll(data, results);
                if (results.Count == 0) { pressed = null; return; }
                data.pointerCurrentRaycast = data.pointerPressRaycast = results[0];
                pressed = ExecuteEvents.ExecuteHierarchy(results[0].gameObject, data, ExecuteEvents.pointerDownHandler)
                    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[0].gameObject);
                data.pointerPress = pressed;
                return;
            }
            if (data == null) return;
            data.position = p;
            if (pressed != null) ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerUpHandler);
            if (dragged != null)
            {
                var results = new List<RaycastResult>();
                es.RaycastAll(data, results);
                if (results.Count > 0) ExecuteEvents.ExecuteHierarchy(results[0].gameObject, data, ExecuteEvents.dropHandler);
                ExecuteEvents.Execute(dragged, data, ExecuteEvents.endDragHandler);
            }
            else if (pressed != null)
            {
                ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerClickHandler);
            }
            pressed = dragged = null;
            data = null;
        }
    }

    public static class PointerInputs
    {
        /// <summary>The best input path in this project: the Input System if it is installed, else UI events.</summary>
        public static IPointerInput Default()
        {
#if VSA_TESTSUITE_INPUT_SYSTEM
            return new InputSystemPointer();
#else
            return new UiEventPointer();
#endif
        }
    }
}
