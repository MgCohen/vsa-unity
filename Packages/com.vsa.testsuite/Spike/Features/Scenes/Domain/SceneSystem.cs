using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite.Spike.Features.Scenes
{
    public sealed class SceneSystem
    {
        public async Task<string> Load(string name, bool additive, CancellationToken cancel)
        {
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var load = SceneManager.LoadSceneAsync(name, mode)
                ?? throw new InvalidOperationException($"Scene '{name}' is not in the build list");
            while (!load.isDone)
                await Awaitable.NextFrameAsync(cancel);
            await Awaitable.NextFrameAsync(cancel);
            return SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name;
        }

        public Task<string> Reset(string name, int? seed, CancellationToken cancel)
        {
            if (seed.HasValue) UnityEngine.Random.InitState(seed.Value);
            var scene = string.IsNullOrEmpty(name) ? SceneManager.GetActiveScene().name : name;
            return Load(scene, false, cancel);
        }
    }
}
