using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    internal sealed class ResetSceneHandler : RequestHandler<ResetScene, SceneResult>
    {
        readonly SceneSystem scenes;

        public ResetSceneHandler(SceneSystem scenes) => this.scenes = scenes;

        public override async Task<Result<SceneResult>> HandleAsync(Envelope<ResetScene> envelope)
        {
            var reset = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var scene = await scenes.Reset(reset.Name, reset.Seed, envelope.Cancellation);
            var frames = Time.frameCount - startFrame;
            return new SceneResult(scene, false, startFrame, frames, Time.realtimeSinceStartup - startTime);
        }
    }
}
