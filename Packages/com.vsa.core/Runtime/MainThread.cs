using System;
using System.Threading;
using UnityEngine;

namespace VSA.Unity {
    // The Unity main thread. The Unity host and the views post work to it.
    // The package code is C# 9: it must compile before the editor script writes Assets/csc.rsp.
    public static class MainThread {
        static SynchronizationContext context;
        static int id = -1;

        public static bool IsCurrent => Thread.CurrentThread.ManagedThreadId == id;

        // Runs work now on the main thread, else posts it to the main thread.
        public static void Post(Action work) {
            if (IsCurrent || context == null) {
                work();
                return;
            }
            context.Post(_ => work(), null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Capture() {
            context = SynchronizationContext.Current;
            id = Thread.CurrentThread.ManagedThreadId;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void CaptureInEditor() => Capture();
#endif
    }
}
