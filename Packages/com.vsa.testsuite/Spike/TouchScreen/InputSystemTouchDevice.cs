#if VSA_SPIKE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace VSA.TestSuite.Spike
{
    /// <summary>Queues mouse state events. The player loop processes them.</summary>
    public sealed class InputSystemTouchDevice : ITouchDevice
    {
        public void Move(Vector2 point) => Queue(point, null, false);

        public void Press(Vector2 point, bool down) => Queue(point, CurrentMouse().leftButton, down);

        // A batch mode Editor has no mouse. Then add one virtual mouse.
        static Mouse CurrentMouse()
        {
            if (Mouse.current != null) return Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>("VSA Spike Mouse");
            mouse.MakeCurrent();
            return mouse;
        }

        static void Queue(Vector2 point, ButtonControl button, bool pressed)
        {
            var mouse = CurrentMouse();
            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(point, eventPtr);
                if (button != null) button.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }
    }
}
#endif
