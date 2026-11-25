using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.StatusChecker.Editor
{
    /// <summary>The main-thread probe of the status checker: "suite ping". The checker calls it with a 2 s
    /// time limit. A queued call that does not start in time gets HTTP 504 and never runs.</summary>
    static class StatusProbe
    {
        [CliGroupCommand("suite", "Main-thread probe of the status checker: frame, flags and dirty scenes")]
        internal static ExitCode Ping(CommandContext cmd)
        {
            var dirty = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) dirty.Add(scene.path.Length > 0 ? scene.path : "(untitled)");
            }
            cmd.Result.WriteRecord(new
            {
                frame = Time.frameCount,
                compiling = EditorApplication.isCompiling,
                updating = EditorApplication.isUpdating,
                playing = EditorApplication.isPlaying,
                paused = EditorApplication.isPaused,
                dirtyScenes = dirty.Count,
                dirtyScenePaths = dirty,
                beacon = StatusBeacon.Enabled,
            });
            return ExitCode.Success;
        }

        [CliGroupCommand("suite", "Turn the status beacon on or off. It stays so until the Editor exits")]
        internal static ExitCode Beacon(CommandContext cmd,
            [CliArg("on", "true: the beacon writes its file each second")] bool on = true)
        {
            StatusBeacon.Enabled = on;
            cmd.Result.WriteRecord(new { beacon = StatusBeacon.Enabled });
            return ExitCode.Success;
        }
    }
}
