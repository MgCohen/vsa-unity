using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;

namespace VSA.TestSuite.Features.Wait
{
    internal sealed class WaitFramesHandler : RequestHandler<WaitFrames, WaitResult>
    {
        public override async Task<Result<WaitResult>> HandleAsync(IEnvelope<WaitFrames> envelope)
        {
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            for (var i = 0; i < envelope.Message.Count; i++)
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            return new WaitResult(Time.frameCount - startFrame, Time.realtimeSinceStartup - startTime);
        }
    }
}
