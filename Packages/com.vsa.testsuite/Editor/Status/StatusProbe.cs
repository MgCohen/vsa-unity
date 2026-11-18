using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite.Editor
{
    /// <summary>The main-thread probe of the status checker: "suite ping". The checker calls it with a 2 s
    /// time limit. A queued call that does not start in time gets HTTP 504 and never runs.</summary>
    static class StatusProbe
    {
        [CliGroupCommand("suite", "Main-thread probe of the status checker: frame, flags and dirty scenes")]
        internal static ExitCode Ping(CommandContext cmd)
        {
            var dirty = 0;
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) dirty++;
            cmd.Result.WriteRecord(new
            {
                frame = Time.frameCount,
                compiling = EditorApplication.isCompiling,
                updating = EditorApplication.isUpdating,
                playing = EditorApplication.isPlaying,
                paused = EditorApplication.isPaused,
                dirtyScenes = dirty,
            });
            return ExitCode.Success;
        }
    }
}
