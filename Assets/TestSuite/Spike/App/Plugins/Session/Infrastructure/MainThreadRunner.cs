using System;
using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class MainThreadRunner : IRunner
    {
        public async Task RunAsync(Envelope envelope, Func<Envelope, Task> run)
        {
            await Awaitable.MainThreadAsync();
            await run(envelope);
        }
    }
}
