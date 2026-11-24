using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VSA.TestSuite
{
    /// <summary>Facts about the running game.</summary>
    public sealed class GameInfo
    {
        public string Platform;
        public bool IsEditor;
        public bool IsPlaying;
        public int Frame;
        public double Time;
        public string Scene;
        public int ProcessId;
    }

    /// <summary>Reads facts about the running game. It runs on the main thread, so it is also a health probe.</summary>
    public sealed class AppSystem
    {
        public GameInfo Info() => new GameInfo
        {
            Platform = Application.platform.ToString(),
            IsEditor = Application.isEditor,
            IsPlaying = Application.isPlaying,
            Frame = UnityEngine.Time.frameCount,
            Time = System.Math.Round(UnityEngine.Time.realtimeSinceStartupAsDouble, 3),
            Scene = SceneManager.GetActiveScene().name,
            ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id,
        };
    }

    [ActionName("game.info")]
    public sealed record ReadInfo : IAction<GameInfo>;

    public sealed class ReadInfoHandler : ActionHandler<ReadInfo, GameInfo>
    {
        readonly AppSystem app;
        public ReadInfoHandler(AppSystem app) => this.app = app;
        protected override Task<GameInfo> Handle(ReadInfo a, ActionContext ctx) => Task.FromResult(app.Info());
    }

    static class InfoCommands
    {
        [CliGroupCommand("game", "Return the platform, frame, time, scene and process id. Runs on the main thread")]
        internal static Task<ExitCode> Info(CommandContext cmd) => Actions.Run(cmd, new ReadInfo());
    }
}
