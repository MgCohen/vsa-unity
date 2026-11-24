using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;

namespace VSA.TestSuite.Spike.Features.Wait
{
    internal sealed class WaitSecondsHandler : RequestHandler<WaitSeconds, WaitResult>
    {
        public override async Task<Result<WaitResult>> HandleAsync(IEnvelope<WaitSeconds> envelope)
        {
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            float seconds;
            while ((seconds = Time.realtimeSinceStartup - startTime) < envelope.Message.Seconds)
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            return new WaitResult(Time.frameCount - startFrame, seconds);
        }
    }
}
