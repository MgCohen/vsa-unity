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

        static Mouse CurrentMouse() => Mouse.current
            ?? throw new ActionFailed(FailCode.NoDevice, "No mouse device in the Input System");

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
        PointerEventData data;

        public string Name => "ui-events";

        public void Move(Vector2 p) { }

        public void Press(Vector2 p, bool down)
        {
            var es = EventSystem.current
                ?? throw new ActionFailed(FailCode.NoDevice, "No EventSystem in the scene for UI events");
            if (down)
            {
                data = new PointerEventData(es) { position = p, button = PointerEventData.InputButton.Left };
                var results = new List<RaycastResult>();
                es.RaycastAll(data, results);
                if (results.Count == 0) { pressed = null; return; }
                data.pointerCurrentRaycast = data.pointerPressRaycast = results[0];
                pressed = ExecuteEvents.ExecuteHierarchy(results[0].gameObject, data, ExecuteEvents.pointerDownHandler)
                    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[0].gameObject);
                data.pointerPress = pressed;
                return;
            }
            if (pressed == null) return;
            ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pressed, data, ExecuteEvents.pointerClickHandler);
            pressed = null;
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
