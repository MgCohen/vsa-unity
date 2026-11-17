using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts.Requests;
using VSA.Presenters;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// What a running script uses. Each Send is one step: it goes through the pipeline as a child of
    /// the script request, so the hooks see it. After each step the script continues on the main thread.
    /// </summary>
    public sealed class ScriptContext
    {
        readonly IBus bus;

        public ScriptContext(IBus bus, string script, CancellationToken cancel)
        {
            this.bus = bus;
            Script = script;
            Cancel = cancel;
        }

        public string Script { get; }

        public CancellationToken Cancel { get; }

        public int Steps { get; private set; }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            Steps++;
            var result = await bus.Send(request, Cancel);
            await Awaitable.MainThreadAsync();
            return result.IsSuccess
                ? result.Value
                : throw new InvalidOperationException($"Script '{Script}' step {Steps} failed: {result.Error}");
        }

        /// <summary>Stops the script with a clear message if the condition is false.</summary>
        public void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Script '{Script}' check failed after step {Steps}: {message}");
        }
    }
}
