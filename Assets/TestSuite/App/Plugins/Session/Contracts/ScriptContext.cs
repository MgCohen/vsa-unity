using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class ScriptContext
    {
        readonly IRequestSender requests;

        public ScriptContext(IRequestSender requests, string script, CancellationToken cancel)
        {
            this.requests = requests;
            Script = script;
            Cancel = cancel;
        }

        public string Script { get; }

        public CancellationToken Cancel { get; }

        public int Steps { get; private set; }

        public async Task<TResponse> Send<TResponse>(TestRequest<TResponse> request)
            where TResponse : IResponse
        {
            Steps++;
            var result = await requests.SendAsync(request, Cancel);
            await Awaitable.MainThreadAsync();
            return result.IsSuccess
                ? result.Value
                : throw new InvalidOperationException($"Script '{Script}' step {Steps} failed: {result.Error}");
        }

        public void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"Script '{Script}' check failed after step {Steps}: {message}");
        }

        public void EndSession() => throw new ScriptEnded(Script);
    }
}
