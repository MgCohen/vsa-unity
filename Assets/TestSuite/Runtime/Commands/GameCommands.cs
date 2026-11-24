using System;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite
{
    /// <summary>CLI commands of the game group. Each one maps its CLI args to one action.</summary>
    static class GameCommands
    {
        const string Group = "game";

        [CliGroupCommand(Group, "Wait for a number of frames")]
        internal static Task<ExitCode> WaitFrames(CommandContext cmd,
            [CliArg("count", "Number of frames")] int count = 1)
            => Actions.Run(cmd, new VSA.TestSuite.WaitFrames(count));

        [CliGroupCommand(Group, "Wait for a time in seconds")]
        internal static Task<ExitCode> WaitSeconds(CommandContext cmd,
            [CliArg("seconds", "Time in seconds")] double seconds = 1)
            => Actions.Run(cmd, new VSA.TestSuite.WaitSeconds(seconds));

        [CliGroupCommand(Group, "Load a scene by name or path, in Play Mode or in a player")]
        internal static Task<ExitCode> LoadScene(CommandContext cmd,
            [CliArg("scene", "Scene name or asset path")] string scene)
            => Actions.Run(cmd, new VSA.TestSuite.LoadScene(scene));

        [CliGroupCommand(Group, "Run the reset steps, set the random seed, and load a fresh scene")]
        internal static Task<ExitCode> Reset(CommandContext cmd,
            [CliArg("scene", "Scene to load. Empty: reload the active scene")] string scene = "",
            [CliArg("seed", "Seed for UnityEngine.Random. Empty: do not change it")] string seed = "")
            => Actions.Run(cmd, new ResetGame(scene, string.IsNullOrEmpty(seed) ? (int?)null : int.Parse(seed, System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>CLI commands of the ui group for waits and asserts.</summary>
    static class UiCheckCommands
    {
        const string Group = "ui";

        [CliGroupCommand(Group, "Wait until an object is Active, Inactive or Missing")]
        internal static Task<ExitCode> WaitForObject(CommandContext cmd,
            [CliArg("path", "Hierarchy path")] string path,
            [CliArg("state", "Active, Inactive or Missing")] string state = "Active",
            [CliArg("timeout-ms", "Time limit in milliseconds")] int timeoutMs = 10000)
            => Actions.Run(cmd, new VSA.TestSuite.WaitForObject(path, ParseState(state), timeoutMs));

        [CliGroupCommand(Group, "Fail if an object is not in the given state")]
        internal static Task<ExitCode> AssertState(CommandContext cmd,
            [CliArg("path", "Hierarchy path")] string path,
            [CliArg("state", "Active, Inactive or Missing")] string state = "Active")
            => Actions.Run(cmd, new VSA.TestSuite.AssertState(path, ParseState(state)));

        [CliGroupCommand(Group, "Fail if the text of an object is not the given text")]
        internal static Task<ExitCode> AssertText(CommandContext cmd,
            [CliArg("path", "Hierarchy path")] string path,
            [CliArg("text", "Expected text")] string text)
            => Actions.Run(cmd, new VSA.TestSuite.AssertText(path, text));

        [CliGroupCommand(Group, "Press an object, keep it down for a time, then release")]
        internal static Task<ExitCode> HoldObject(CommandContext cmd,
            [CliArg("path", "Hierarchy path")] string path,
            [CliArg("ms", "Time to keep the button down")] int ms = 500)
            => Actions.Run(cmd, new VSA.TestSuite.HoldObject(path, ms));

        [CliGroupCommand(Group, "Press a screen point, keep it down for a time, then release")]
        internal static Task<ExitCode> HoldScreen(CommandContext cmd,
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y,
            [CliArg("ms", "Time to keep the button down")] int ms = 500)
            => Actions.Run(cmd, new VSA.TestSuite.HoldScreen(x, y, ms));

        [CliGroupCommand(Group, "Drag from one object to another")]
        internal static Task<ExitCode> DragObject(CommandContext cmd,
            [CliArg("path", "Hierarchy path of the object to drag")] string path,
            [CliArg("to", "Hierarchy path of the target object")] string to,
            [CliArg("steps", "Number of move frames")] int steps = 10)
            => Actions.Run(cmd, new VSA.TestSuite.DragObject(path, to, steps));

        [CliGroupCommand(Group, "Drag from one screen point to another")]
        internal static Task<ExitCode> DragScreen(CommandContext cmd,
            [CliArg("x", "Start, pixels from the left")] float x,
            [CliArg("y", "Start, pixels from the bottom")] float y,
            [CliArg("to-x", "End, pixels from the left")] float toX,
            [CliArg("to-y", "End, pixels from the bottom")] float toY,
            [CliArg("steps", "Number of move frames")] int steps = 10)
            => Actions.Run(cmd, new VSA.TestSuite.DragScreen(x, y, toX, toY, steps));

        static ObjectState ParseState(string s) => (ObjectState)Enum.Parse(typeof(ObjectState), s, true);
    }
}
