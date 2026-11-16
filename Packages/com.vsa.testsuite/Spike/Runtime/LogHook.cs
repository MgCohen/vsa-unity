using System;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>Writes one console line when an action starts and one when it ends.</summary>
    internal sealed class LogHook : IActionHook
    {
        public const string Tag = "[Spike]";
        int startFrame;

        public void Before(object action)
        {
            startFrame = Time.frameCount;
            Debug.Log($"{Tag} start {action} frame={startFrame}");
        }

        public void After(object action, object result, Exception error)
        {
            int frames = Time.frameCount - startFrame;
            if (error == null) Debug.Log($"{Tag} done {action.GetType().Name} frames={frames} result={result}");
            else Debug.LogWarning($"{Tag} failed {action.GetType().Name} frames={frames} error={error.Message}");
        }
    }
}
