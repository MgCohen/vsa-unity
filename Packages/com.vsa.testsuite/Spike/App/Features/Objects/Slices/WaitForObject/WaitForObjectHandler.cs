using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class WaitForObjectHandler : RequestHandler<WaitForObject, WaitForResult>
    {
        readonly ObjectSystem objects;

        public WaitForObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override async Task<Result<WaitForResult>> HandleAsync(IEnvelope<WaitForObject> envelope)
        {
            var wait = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            while (true)
            {
                var state = objects.State(wait.Path);
                var seconds = Time.realtimeSinceStartup - startTime;
                if (state == wait.State)
                    return new WaitForResult(wait.Path, state, Time.frameCount - startFrame, seconds);
                if (seconds >= wait.Seconds)
                    throw new InvalidOperationException(
                        $"'{wait.Path}' is {state}, not {wait.State}, after {wait.Seconds} s");
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            }
        }
    }
}
