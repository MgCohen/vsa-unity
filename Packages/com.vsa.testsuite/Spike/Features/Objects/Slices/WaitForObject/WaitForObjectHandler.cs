using System;
using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Looks for the object one time in each frame. It stops when the object is found, when the time
    /// is over, or when the envelope token cancels.
    /// It does not throw TimeoutException: the Pipeline reads that type as its own command timeout.
    /// </summary>
    internal sealed class WaitForObjectHandler : RequestHandler<WaitForObject, WaitResult>
    {
        readonly ObjectSystem objects;

        public WaitForObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override async Task<Result<WaitResult>> HandleAsync(Envelope<WaitForObject> envelope)
        {
            await Awaitable.MainThreadAsync();
            var wait = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                var target = objects.TryFind(wait.Path);
                var seconds = Time.realtimeSinceStartup - startTime;
                if (target != null)
                    return new WaitResult(wait.Path, target.name, Time.frameCount - startFrame, seconds);
                if (seconds >= wait.Seconds)
                    throw new InvalidOperationException($"No active object at '{wait.Path}' after {wait.Seconds} s");
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            }
        }
    }
}
