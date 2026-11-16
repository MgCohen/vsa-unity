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

        static ObjectState ParseState(string s) => (ObjectState)Enum.Parse(typeof(ObjectState), s, true);
    }
}
