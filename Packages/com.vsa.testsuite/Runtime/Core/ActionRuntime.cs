using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite
{
    /// <summary>
    /// The static entry for CLI commands. It only finds the active runtime.
    /// </summary>
    public static class Actions
    {
        public static Task<ExitCode> Run<TResult>(CommandContext cmd, IAction<TResult> action)
            => ActionRuntime.GetOrBuild().RunFromCli(cmd, action);
    }

    /// <summary>
    /// Holds the action pipeline and the run folder. The first call builds it.
    /// </summary>
    public sealed class ActionRuntime
    {
        /// <summary>Builds a runtime. Set it before the first command to use another composition root.</summary>
        public static Func<ActionRuntime> Factory = () => new TestRoot().Build();

        static ActionRuntime active;

        readonly ActionPipeline pipeline;
        int stepCount;

        public RunFolder Run { get; }
        public DateTime BuiltAt { get; } = DateTime.Now;

        public ActionRuntime(ActionPipeline pipeline, RunFolder run)
        {
            this.pipeline = pipeline;
            Run = run;
        }

        /// <summary>Returns the active runtime. If there is none, builds it from the composition root.</summary>
        public static ActionRuntime GetOrBuild()
        {
            if (active == null)
            {
                active = Factory();
                UnityEngine.Debug.Log("[TestSuite] Runtime built. Run folder: " + active.Run.Path);
            }
            return active;
        }

        public static bool IsBuilt => active != null;

        /// <summary>For tests of the lazy start only: forget the active runtime.</summary>
        public static void Reset() => active = null;

        /// <summary>Runs one action as one step. Callers inside Unity use this method.</summary>
        /// <param name="script">The name of the script that runs the step, for the run summary. Null: no script.</param>
        public Task<TResult> RunStep<TResult>(IAction<TResult> action, CancellationToken cancel, string script = null)
        {
            var step = Run.NewStep(++stepCount, ActionNames.Of(action.GetType()), script);
            return pipeline.Run(action, null, cancel, step);
        }

        /// <summary>Runs one action for a CLI command, and maps the result to the CLI result and exit code.</summary>
        public async Task<ExitCode> RunFromCli<TResult>(CommandContext cmd, IAction<TResult> action)
        {
            var step = Run.NewStep(++stepCount, ActionNames.Of(action.GetType()));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await pipeline.Run(action, null, cmd.CancellationToken, step);
                cmd.Result.Write(new
                {
                    ok = true,
                    action = ActionNames.Of(action.GetType()),
                    step = step.Id,
                    ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1),
                    folder = step.Path,
                    result,
                });
                return ExitCode.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ActionFailed f)
            {
                cmd.Log.Error(f.Code + ": " + f.Message);
                cmd.Result.Write(new
                {
                    ok = false,
                    action = ActionNames.Of(action.GetType()),
                    step = step.Id,
                    ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1),
                    code = f.Code.ToString(),
                    reason = f.Message,
                    detail = f.Detail,
                    folder = step.Path,
                });
                return f.Code == FailCode.NotFound ? ExitCode.NotFound
                    : f.Code == FailCode.TimedOut ? ExitCode.TimedOut
                    : ExitCode.Failed;
            }
        }
    }
}
