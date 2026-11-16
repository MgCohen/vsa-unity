using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>
    /// Records a video of each <c>suite.run</c> step into its step folder. The stop is in After,
    /// and the pipeline always calls After for a hook whose Before ran: pass, fail, cancel or exception.
    /// </summary>
    public sealed class RecordScriptHook : IActionHook
    {
        readonly RecordingSystem recording;
        ActionContext owner;

        public RecordScriptHook(RecordingSystem recording) => this.recording = recording;

        public Task Before(ActionContext ctx)
        {
            if (ctx.Depth != 0 || !(ctx.Action is RunScript run)) return Task.CompletedTask;
            if (!recording.Options.RecordScripts || !Application.isPlaying) return Task.CompletedTask;
            if (recording.Unavailable != null) return Task.CompletedTask;
            try
            {
                recording.Start(ctx.Step.Path, run.Script, ctx.Step.Id);
                owner = ctx;
            }
            catch (System.Exception e)
            {
                // A recorder problem must not fail the test. The log has the reason.
                Debug.LogWarning("[TestSuite] Recording did not start: " + e.Message);
            }
            return Task.CompletedTask;
        }

        public Task After(ActionContext ctx)
        {
            if (owner != ctx) return Task.CompletedTask;
            owner = null;
            recording.Stop(ctx.Outcome.Failed ? StopReason.StepFailed : StopReason.Stopped);
            return Task.CompletedTask;
        }
    }

    /// <summary>The state of the recording system.</summary>
    public sealed class RecordingStatus
    {
        public string Recorder;
        public string Unavailable;
        public Recording Current;
        public Recording Last;
    }

    [ActionName("record.start")]
    public sealed record StartRecording(string Name, int? MaxSeconds = null) : IAction<Recording>;

    [ActionName("record.stop")]
    public sealed record StopRecording : IAction<Recording>;

    [ActionName("record.status")]
    public sealed record ReadRecording : IAction<RecordingStatus>;

    public sealed class StartRecordingHandler : ActionHandler<StartRecording, Recording>
    {
        readonly RecordingSystem recording;
        public StartRecordingHandler(RecordingSystem recording) => this.recording = recording;

        protected override Task<Recording> Handle(StartRecording a, ActionContext ctx)
        {
            if (!Application.isPlaying) throw new ActionFailed(FailCode.NotActive, "Recording needs Play Mode or a player");
            return Task.FromResult(recording.Start(ctx.Step.Path, string.IsNullOrEmpty(a.Name) ? "session" : a.Name, ctx.Step.Id, a.MaxSeconds));
        }
    }

    public sealed class StopRecordingHandler : ActionHandler<StopRecording, Recording>
    {
        readonly RecordingSystem recording;
        public StopRecordingHandler(RecordingSystem recording) => this.recording = recording;
        protected override Task<Recording> Handle(StopRecording a, ActionContext ctx) => Task.FromResult(recording.Stop());
    }

    public sealed class ReadRecordingHandler : ActionHandler<ReadRecording, RecordingStatus>
    {
        readonly RecordingSystem recording;
        public ReadRecordingHandler(RecordingSystem recording) => this.recording = recording;

        protected override Task<RecordingStatus> Handle(ReadRecording a, ActionContext ctx) => Task.FromResult(new RecordingStatus
        {
            Recorder = recording.RecorderName,
            Unavailable = recording.Unavailable,
            Current = recording.Current,
            Last = recording.Last,
        });
    }

    /// <summary>CLI commands of the record group.</summary>
    static class RecordCommands
    {
        const string Group = "record";

        [CliGroupCommand(Group, "Start a video recording. A running recording stops first")]
        internal static Task<ExitCode> Start(CommandContext cmd,
            [CliArg("name", "The file name, with no extension")] string name = "session",
            [CliArg("max-seconds", "The time limit of this recording. 0: the default limit")] int maxSeconds = 0)
            => Actions.Run(cmd, new StartRecording(name, maxSeconds > 0 ? maxSeconds : (int?)null));

        [CliGroupCommand(Group, "Stop the video recording. With no recording, it returns the last one")]
        internal static Task<ExitCode> Stop(CommandContext cmd)
            => Actions.Run(cmd, new StopRecording());

        [CliGroupCommand(Group, "Show the recorder, the current recording and the last recording")]
        internal static Task<ExitCode> Status(CommandContext cmd)
            => Actions.Run(cmd, new ReadRecording());
    }
}
