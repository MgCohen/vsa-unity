using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite
{
    /// <summary>The result of a scene load or a reset.</summary>
    public sealed class SceneResult
    {
        public string Scene;
        public string Path;
        public double Ms;
        public int? Seed;
        public string[] ResetSteps;
    }

    /// <summary>
    /// One step of a reset, for state outside the scene: save data, static caches, services.
    /// A consumer adds steps to the reset system in the composition root.
    /// </summary>
    public interface IResetStep
    {
        string Name { get; }
        Task Run(CancellationToken cancel);
    }

    /// <summary>Loads scenes in Play Mode and in players. In the Editor it also loads scenes that are not in the build list.</summary>
    public sealed class SceneSystem
    {
        public string Active() => SceneManager.GetActiveScene().name;

        public async Task<SceneResult> Load(string nameOrPath, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
                throw new ActionFailed(FailCode.Failed, "No scene name or path");
            if (!Application.isPlaying)
                throw new ActionFailed(FailCode.Failed, "A scene load needs Play Mode or a player");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var op = Start(nameOrPath)
                ?? throw new ActionFailed(FailCode.NotFound, "No scene '" + nameOrPath + "' in the build list or in the project");
            while (!op.isDone)
            {
                cancel.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            // The new scene runs Awake and Start in the next frame.
            await Frames.Wait(1, cancel);
            var scene = SceneManager.GetActiveScene();
            return new SceneResult { Scene = scene.name, Path = scene.path, Ms = System.Math.Round(watch.Elapsed.TotalMilliseconds, 1) };
        }

        public Task<SceneResult> Reload(CancellationToken cancel)
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) && string.IsNullOrEmpty(scene.name))
                throw new ActionFailed(FailCode.Failed, "The active scene is not saved, so it cannot reload. Give the scene to load.");
            return Load(string.IsNullOrEmpty(scene.path) ? scene.name : scene.path, cancel);
        }

        static AsyncOperation Start(string nameOrPath)
        {
            if (Application.CanStreamedLevelBeLoaded(nameOrPath))
                return SceneManager.LoadSceneAsync(nameOrPath, LoadSceneMode.Single);
#if UNITY_EDITOR
            var path = nameOrPath.EndsWith(".unity") ? nameOrPath : FindScenePath(nameOrPath);
            if (path != null)
                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            return null;
        }

#if UNITY_EDITOR
        static string FindScenePath(string name)
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Scene " + name))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return path;
            }
            return null;
        }
#endif
    }

    /// <summary>Puts the game in a known state: consumer reset steps, a fixed seed, then a fresh scene.</summary>
    public sealed class ResetSystem
    {
        readonly SceneSystem scenes;
        readonly List<IResetStep> steps = new List<IResetStep>();

        public ResetSystem(SceneSystem scenes) => this.scenes = scenes;

        public ResetSystem Add(IResetStep step)
        {
            steps.Add(step);
            return this;
        }

        /// <param name="scene">The scene to load. Null or empty: reload the active scene.</param>
        /// <param name="seed">The seed for UnityEngine.Random. Null: do not change it.</param>
        public async Task<SceneResult> Reset(string scene, int? seed, CancellationToken cancel)
        {
            var names = new List<string>();
            foreach (var step in steps)
            {
                cancel.ThrowIfCancellationRequested();
                await step.Run(cancel);
                names.Add(step.Name);
            }
            if (seed.HasValue) Random.InitState(seed.Value);
            var result = string.IsNullOrEmpty(scene) ? await scenes.Reload(cancel) : await scenes.Load(scene, cancel);
            result.Seed = seed;
            result.ResetSteps = names.ToArray();
            return result;
        }
    }
}
