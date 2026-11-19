using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class WaitForObjectHandler : RequestHandler<WaitForObject, WaitResult>
    {
        readonly ObjectSystem objects;

        public WaitForObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override async Task<Result<WaitResult>> HandleAsync(Envelope<WaitForObject> envelope)
        {
            var wait = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var pinged = 0;
            while (true)
            {
                var target = objects.TryFind(wait.Path);
                var seconds = Time.realtimeSinceStartup - startTime;
                if (target != null)
                    return new WaitResult(wait.Path, target.name, Time.frameCount - startFrame, seconds);
                if (seconds >= wait.Seconds)
                    throw new InvalidOperationException($"No active object at '{wait.Path}' after {wait.Seconds} s");
                if ((int)seconds > pinged)
                    SessionProgress.Ping($"wait for {wait.Path}, {pinged = (int)seconds} s");
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            }
        }
    }
}
