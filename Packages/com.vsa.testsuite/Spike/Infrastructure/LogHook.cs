using System;
using System.Collections.Generic;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Writes one console line when an action starts and one when it ends. A sub-action
    /// gets an indent, so the log shows the nesting.
    /// </summary>
    internal sealed class LogHook : IActionHook
    {
        public const string Tag = "[Spike]";
        readonly Stack<int> startFrames = new Stack<int>();

        string Indent => new string(' ', startFrames.Count * 2);

        public void Before(object action)
        {
            Debug.Log($"{Tag} {Indent}start {action} frame={Time.frameCount}");
            startFrames.Push(Time.frameCount);
        }

        public void After(object action, object result, Exception error)
        {
            int frames = Time.frameCount - startFrames.Pop();
            if (error == null) Debug.Log($"{Tag} {Indent}done {action.GetType().Name} frames={frames} result={result}");
            else Debug.LogWarning($"{Tag} {Indent}failed {action.GetType().Name} frames={frames} error={error.Message}");
        }
    }
}
