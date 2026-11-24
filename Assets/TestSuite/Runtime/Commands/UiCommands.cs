using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite
{
    /// <summary>CLI commands of the ui group. Each one maps its CLI args to one action.</summary>
    static class UiCommands
    {
        const string Group = "ui";

        [CliGroupCommand(Group, "Find an object by hierarchy path and return its screen point")]
        internal static Task<ExitCode> FindObject(CommandContext cmd,
            [CliArg("path", "Hierarchy path, for example Canvas/Menu/Play")] string path)
            => Actions.Run(cmd, new VSA.TestSuite.FindObject(path));

        [CliGroupCommand(Group, "Click a screen point in pixels")]
        internal static Task<ExitCode> ClickScreen(CommandContext cmd,
            [CliArg("x", "Pixels from the left")] float x,
            [CliArg("y", "Pixels from the bottom")] float y)
            => Actions.Run(cmd, new VSA.TestSuite.ClickScreen(x, y));

        [CliGroupCommand(Group, "Click an object by hierarchy path")]
        internal static Task<ExitCode> ClickObject(CommandContext cmd,
            [CliArg("path", "Hierarchy path, for example Canvas/Menu/Play")] string path)
            => Actions.Run(cmd, new VSA.TestSuite.ClickObject(path));
    }
}
