using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>Manual composition: builds the systems, adds the handlers and the hooks.</summary>
    internal static class Composition
    {
        public static ActionRuntime Build()
        {
            var device = TouchDevice();
            Debug.Log($"{LogHook.Tag} runtime built, device={device.GetType().Name}");
            var touch = new TouchSystem(device);
            var registry = new ActionRegistry()
                .Add(new TouchScreenHandler(touch));
            return new ActionRuntime(registry, new IActionHook[] { new LogHook() });
        }

        static ITouchDevice TouchDevice()
        {
#if VSA_SPIKE_INPUT_SYSTEM
            return new InputSystemTouchDevice();
#else
            return new UiEventTouchDevice();
#endif
        }
    }
}
