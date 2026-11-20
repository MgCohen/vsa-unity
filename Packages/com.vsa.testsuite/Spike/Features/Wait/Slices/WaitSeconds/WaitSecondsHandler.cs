using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Wait
{
    internal sealed class WaitSecondsHandler : RequestHandler<WaitSeconds, WaitResult>
    {
        public override async Task<Result<WaitResult>> HandleAsync(Envelope<WaitSeconds> envelope)
        {
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var pinged = 0;
            float seconds;
            while ((seconds = Time.realtimeSinceStartup - startTime) < envelope.Message.Seconds)
            {
                if ((int)seconds > pinged)
                    SessionProgress.Ping($"wait {envelope.Message.Seconds} s, {pinged = (int)seconds} s");
                await Awaitable.NextFrameAsync(envelope.Cancellation);
            }
            return new WaitResult(Time.frameCount - startFrame, seconds);
        }
    }
}
