using System;
using System.Collections.Generic;

namespace VSA.TestSuite.Spike
{
    /// <summary>Maps each action type to its handler.</summary>
    internal sealed class ActionRegistry
    {
        readonly Dictionary<Type, IActionHandler> handlers = new Dictionary<Type, IActionHandler>();

        public ActionRegistry Add(IActionHandler handler)
        {
            handlers.Add(handler.ActionType, handler);
            return this;
        }

        public IActionHandler Find(Type actionType)
            => handlers.TryGetValue(actionType, out var handler)
                ? handler
                : throw new InvalidOperationException($"No handler for {actionType.Name}");
    }
}
