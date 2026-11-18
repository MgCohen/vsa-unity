using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace VSA.TestSuite.Client.Tests
{
    /// <summary>One test for each CLI command: arguments in, envelope and exit code out.
    /// The project is an empty folder with ProjectVersion.txt, so no Editor runs on it.</summary>
    public sealed class CliTests : IDisposable
    {
        readonly string project = Path.Combine(Path.GetTempPath(), "vsa-cli-" + Guid.NewGuid().ToString("N"));

        public CliTests()
        {
            Directory.CreateDirectory(Path.Combine(project, "ProjectSettings"));
            File.WriteAllText(Path.Combine(project, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.6.6f1\n");
            var run = Path.Combine(project, "Logs", "vsa-unity", "20261118-120000-abcdef");
            Directory.CreateDirectory(run);
            File.WriteAllText(Path.Combine(run, "unity.log"), "first\nsecond\n");
        }

        public void Dispose()
        {
            try { Directory.Delete(project, true); }
            catch (IOException) { }
        }

        (int Exit, JsonElement Envelope) Run(params string[] args)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = project,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "vsa-unity.dll"));
            foreach (var a in args) info.ArgumentList.Add(a);
            using var p = Process.Start(info);
            var stdout = p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit();
            var last = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last();
            return (p.ExitCode, JsonDocument.Parse(last).RootElement.Clone());
        }

        static string Code(JsonElement envelope) => envelope.GetProperty("errors")[0].GetProperty("code").GetString();

        [Fact]
        public void Help_succeeds()
        {
            var (exit, e) = Run("help");
            Assert.Equal(0, exit);
            Assert.True(e.GetProperty("success").GetBoolean());
        }

        [Fact]
        public void Status_with_no_editor_reports_no_editor_and_no_history()
        {
            var (exit, e) = Run("status");
            Assert.Equal(0, exit);
            Assert.Equal("no-editor", e.GetProperty("data").GetProperty("state").GetString());
            Assert.False(e.GetProperty("data").GetProperty("history").GetBoolean());
        }

        [Fact]
        public void Wait_with_no_editor_exits_12()
        {
            var (exit, e) = Run("wait", "--timeout", "5");
            Assert.Equal(12, exit);
            Assert.Equal("NoEditor", Code(e));
        }

        [Theory]
        [InlineData("wait", "--until", "sleeping")]
        [InlineData("wait", "--timeout", "0")]
        [InlineData("watch", "--pid", "1")]
        [InlineData("log", "show", "--run", "nope")]
        [InlineData("fly")]
        public void Bad_arguments_exit_13(params string[] args)
        {
            var (exit, e) = Run(args);
            Assert.Equal(13, exit);
            Assert.False(e.GetProperty("success").GetBoolean());
        }

        [Fact]
        public void Log_commands_list_show_and_mark_the_run()
        {
            var (exit, e) = Run("log", "sessions");
            Assert.Equal(0, exit);
            Assert.Equal("20261118-120000-abcdef", e.GetProperty("data").GetProperty("runs")[0].GetProperty("id").GetString());

            (exit, _) = Run("log", "mark", "step A7");
            Assert.Equal(0, exit);

            (exit, e) = Run("log", "show", "--tail", "1");
            Assert.Equal(0, exit);
            Assert.Equal("second", e.GetProperty("data").GetProperty("unityLog")[0].GetString());
            Assert.Contains("step A7", e.GetProperty("data").GetProperty("watchLog")[0].GetString());
        }

        [Fact]
        public void Config_show_names_the_source_of_each_value()
        {
            var (exit, e) = Run("config", "show", "--keep-runs", "5");
            Assert.Equal(0, exit);
            Assert.Equal("option --keep-runs", e.GetProperty("data").GetProperty("keepRuns").GetProperty("source").GetString());
            Assert.Equal("default", e.GetProperty("data").GetProperty("logRoot").GetProperty("source").GetString());
        }
    }
}
