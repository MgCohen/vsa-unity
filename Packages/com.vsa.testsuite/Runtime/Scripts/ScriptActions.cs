using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite
{
    [ActionName("suite.run")]
    public sealed record RunScript(string Script) : IAction<ScriptResult>;

    [ActionName("suite.list")]
    public sealed record ListScripts : IAction<string[]>;

    public sealed class RunScriptHandler : ActionHandler<RunScript, ScriptResult>
    {
        readonly ScriptSystem scripts;
        public RunScriptHandler(ScriptSystem scripts) => this.scripts = scripts;
        protected override Task<ScriptResult> Handle(RunScript a, ActionContext ctx) => scripts.Run(a.Script, ctx.Cancel);
    }

    public sealed class ListScriptsHandler : ActionHandler<ListScripts, string[]>
    {
        readonly ScriptSystem scripts;
        public ListScriptsHandler(ScriptSystem scripts) => this.scripts = scripts;
        protected override Task<string[]> Handle(ListScripts a, ActionContext ctx) => Task.FromResult(scripts.Names());
    }

    /// <summary>CLI commands of the suite group.</summary>
    static class SuiteCommands
    {
        const string Group = "suite";

        [CliGroupCommand(Group, "Run a C# test script. Each step writes its own evidence")]
        internal static Task<ExitCode> Run(CommandContext cmd,
            [CliArg("script", "The script name")] string script)
            => Actions.Run(cmd, new RunScript(script));

        [CliGroupCommand(Group, "List the C# test scripts")]
        internal static Task<ExitCode> List(CommandContext cmd)
            => Actions.Run(cmd, new ListScripts());
    }
}
