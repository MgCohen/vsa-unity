using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>The folder of one runtime session: one sub-folder for each step.</summary>
    public sealed class RunFolder
    {
        public string Path { get; }

        public RunFolder(string root)
        {
            Path = System.IO.Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd_HHmmss"));
            Directory.CreateDirectory(Path);
        }

        /// <summary>Outside Assets and Temp in the Editor; persistentDataPath in a player.</summary>
        public static string DefaultRoot()
            => Application.isEditor
                ? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Runs"))
                : System.IO.Path.Combine(Application.persistentDataPath, "Runs");

        public StepFolder NewStep(int index, string actionName, string script = null)
        {
            var id = "s-" + index.ToString("000") + "_" + actionName.Replace('.', '-');
            return new StepFolder(id, System.IO.Path.Combine(Path, id), this, script);
        }
    }

    /// <summary>The folder of one step. It is made only when a hook writes to it.</summary>
    public sealed class StepFolder
    {
        public string Id { get; }
        public string Path { get; }
        /// <summary>The run folder of the step. Null in unit tests.</summary>
        public RunFolder Run { get; }
        /// <summary>The script that runs the step. Null: a single command.</summary>
        public string Script { get; }

        public StepFolder(string id, string path, RunFolder run = null, string script = null)
        {
            Id = id;
            Path = path;
            Run = run;
            Script = script;
        }

        public string File(string name)
        {
            Directory.CreateDirectory(Path);
            return System.IO.Path.Combine(Path, name);
        }
    }

    /// <summary>Waits for frames of the player loop.</summary>
    public static class Frames
    {
        public static async Task Wait(int count, CancellationToken cancel, int timeoutMs = 5000)
        {
            int target = Time.frameCount + count;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (Time.frameCount < target)
            {
                cancel.ThrowIfCancellationRequested();
                if (watch.ElapsedMilliseconds > timeoutMs)
                    throw new ActionFailed(FailCode.TimedOut, "The player loop did not advance " + count + " frames in " + timeoutMs + " ms");
                await Task.Yield();
            }
        }
    }
}
