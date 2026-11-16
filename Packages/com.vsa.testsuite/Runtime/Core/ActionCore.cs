using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VSA.TestSuite
{
    /// <summary>An action with no value to return.</summary>
    public readonly struct Unit
    {
        public static readonly Unit Value = default;
    }

    /// <summary>Marks a class as an action. An action is data only.</summary>
    public interface IAction { }

    /// <summary>An action that returns a value of type <typeparamref name="TResult"/>.</summary>
    public interface IAction<TResult> : IAction { }

    /// <summary>Gives an action its DSL name, for example "ui.click_object".</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ActionNameAttribute : Attribute
    {
        public string Name { get; }
        public ActionNameAttribute(string name) => Name = name;
    }

    /// <summary>Codes for failures that an action or a system reports.</summary>
    public enum FailCode
    {
        Failed,
        NotFound,
        NotActive,
        Blocked,
        NoDevice,
        NoHandler,
        TimedOut,
        AssertFailed,
    }

    /// <summary>A failure with a code and a reason. Systems throw it; the runtime maps it to an exit code.</summary>
    public sealed class ActionFailed : Exception
    {
        public FailCode Code { get; }
        public object Detail { get; }

        public ActionFailed(FailCode code, string reason, object detail = null) : base(reason)
        {
            Code = code;
            Detail = detail;
        }
    }

    /// <summary>What happened to one action. Hooks receive it after the action.</summary>
    public sealed class ActionOutcome
    {
        public object Result;
        public Exception Error;
        public double Ms;
        public bool Failed => Error != null;
    }

    /// <summary>One node in the tree of running actions.</summary>
    public sealed class ActionContext
    {
        readonly ActionPipeline pipeline;
        readonly Stopwatch watch = Stopwatch.StartNew();

        public IAction Action { get; }
        public string Name { get; }
        public ActionContext Parent { get; }
        public int Depth { get; }
        public CancellationToken Cancel { get; }
        public StepFolder Step { get; }
        public List<ActionContext> Children { get; } = new List<ActionContext>();
        public ActionOutcome Outcome { get; internal set; }

        internal ActionContext(ActionPipeline pipeline, IAction action, string name, ActionContext parent,
            CancellationToken cancel, StepFolder step)
        {
            this.pipeline = pipeline;
            Action = action;
            Name = name;
            Parent = parent;
            Depth = parent == null ? 0 : parent.Depth + 1;
            Cancel = cancel;
            Step = step;
            parent?.Children.Add(this);
        }

        public double ElapsedMs => watch.Elapsed.TotalMilliseconds;

        /// <summary>Runs a child action through the pipeline, so the hooks apply to it too.</summary>
        public Task<TResult> Run<TResult>(IAction<TResult> action) => pipeline.Run(action, this, Cancel, Step);
    }

    /// <summary>Code that runs before and after each action, nested actions too.</summary>
    public interface IActionHook
    {
        Task Before(ActionContext ctx);
        Task After(ActionContext ctx);
    }

    /// <summary>Untyped view of a handler, for the handler set.</summary>
    public interface IActionHandler
    {
        Type ActionType { get; }
        Task<object> HandleUntyped(IAction action, ActionContext ctx);
    }

    /// <summary>Base for action handlers. A handler only routes: it calls one system or other actions.</summary>
    public abstract class ActionHandler<TAction, TResult> : IActionHandler where TAction : IAction<TResult>
    {
        public Type ActionType => typeof(TAction);

        public async Task<object> HandleUntyped(IAction action, ActionContext ctx) => await Handle((TAction)action, ctx);

        protected abstract Task<TResult> Handle(TAction action, ActionContext ctx);
    }

    /// <summary>Finds the handler for each action type.</summary>
    public sealed class HandlerSet
    {
        readonly Dictionary<Type, IActionHandler> handlers = new Dictionary<Type, IActionHandler>();

        public HandlerSet Add(IActionHandler handler)
        {
            handlers[handler.ActionType] = handler;
            return this;
        }

        public IActionHandler Find(Type actionType) => handlers.TryGetValue(actionType, out var h) ? h : null;
    }

    /// <summary>Runs each action with the hooks around it.</summary>
    public sealed class ActionPipeline
    {
        readonly HandlerSet handlers;
        readonly IReadOnlyList<IActionHook> hooks;

        public ActionPipeline(HandlerSet handlers, IReadOnlyList<IActionHook> hooks)
        {
            this.handlers = handlers;
            this.hooks = hooks;
        }

        public async Task<TResult> Run<TResult>(IAction<TResult> action, ActionContext parent,
            CancellationToken cancel, StepFolder step)
        {
            var ctx = new ActionContext(this, action, ActionNames.Of(action.GetType()), parent, cancel, step);
            var outcome = new ActionOutcome();
            foreach (var hook in hooks) await hook.Before(ctx);
            try
            {
                cancel.ThrowIfCancellationRequested();
                var handler = handlers.Find(action.GetType())
                    ?? throw new ActionFailed(FailCode.NoHandler, "No handler for action " + ctx.Name);
                outcome.Result = await handler.HandleUntyped(action, ctx);
                return (TResult)outcome.Result;
            }
            catch (Exception e)
            {
                outcome.Error = e;
                throw;
            }
            finally
            {
                outcome.Ms = ctx.ElapsedMs;
                ctx.Outcome = outcome;
                for (int i = hooks.Count - 1; i >= 0; i--)
                {
                    try { await hooks[i].After(ctx); }
                    catch (Exception e) { UnityEngine.Debug.LogException(e); }
                }
            }
        }
    }

    public static class ActionNames
    {
        static readonly Dictionary<Type, string> cache = new Dictionary<Type, string>();

        public static string Of(Type type)
        {
            if (cache.TryGetValue(type, out var name)) return name;
            var attr = (ActionNameAttribute)Attribute.GetCustomAttribute(type, typeof(ActionNameAttribute));
            name = attr != null ? attr.Name : type.Name;
            cache[type] = name;
            return name;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // Lets C# records and init accessors compile on the Unity profile. It is public, so
    // assemblies that reference this package can also declare actions as records.
    public static class IsExternalInit { }
}
