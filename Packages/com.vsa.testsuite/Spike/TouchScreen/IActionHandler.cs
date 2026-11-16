using System;
using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>The untyped side of a handler, so the registry can keep all handlers in one map.</summary>
    internal interface IActionHandler
    {
        Type ActionType { get; }
        Task<object> Run(object action, ActionContext context);
    }
}
