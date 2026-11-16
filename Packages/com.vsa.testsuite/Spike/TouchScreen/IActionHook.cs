using System;

namespace VSA.TestSuite.Spike
{
    /// <summary>Runs before and after each action. A hook must not change the action.</summary>
    internal interface IActionHook
    {
        void Before(object action);
        void After(object action, object result, Exception error);
    }
}
