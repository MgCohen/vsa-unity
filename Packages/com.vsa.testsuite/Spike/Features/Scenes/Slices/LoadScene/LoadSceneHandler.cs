using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    internal sealed class LoadSceneHandler : RequestHandler<LoadScene, SceneResult>
    {
        readonly SceneSystem scenes;

        public LoadSceneHandler(SceneSystem scenes) => this.scenes = scenes;

        public override async Task<Result<SceneResult>> HandleAsync(IEnvelope<LoadScene> envelope)
        {
            var load = envelope.Message;
            var startFrame = Time.frameCount;
            var startTime = Time.realtimeSinceStartup;
            var scene = await scenes.Load(load.Name, load.Additive, envelope.Cancellation);
            var frames = Time.frameCount - startFrame;
            return new SceneResult(scene, load.Additive, startFrame, frames, Time.realtimeSinceStartup - startTime);
        }
    }
}
