using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class WaitForObjectHandler : RequestHandler<WaitForObject, WaitForResult>
    {
        readonly ObjectSystem objects;

        public WaitForObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override async Task<Result<WaitForResult>> HandleAsync(Envelope<WaitForObject> envelope)
        {
            var wait = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var pinged = 0;
            while (true)
            {
                var state = objects.State(wait.Path);
                var seconds = Time.realtimeSinceStartup - startTime;
                if (state == wait.State)
                    return new WaitForResult(wait.Path, state, Time.frameCount - startFrame, seconds);
                if (seconds >= wait.Seconds)
                    throw new InvalidOperationException(
                        $"'{wait.Path}' is {state}, not {wait.State}, after {wait.Seconds} s");
                if ((int)seconds > pinged)
                    SessionProgress.Ping($"wait for {wait.Path} to be {wait.State}, {pinged = (int)seconds} s");
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            }
        }
    }
}
