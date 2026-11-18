using VSA.TestSuite.Client.Status;
using Xunit;

namespace VSA.TestSuite.Client.Tests
{
    public sealed class RunLogTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "vsa-runlog-" + Guid.NewGuid().ToString("N"));

        public RunLogTests()
        {
            foreach (var id in new[] { "20261118-100000-aaaaaa", "20261118-110000-bbbbbb", "precheck-20261118-115900", "20261118-120000-cccccc" })
            {
                Directory.CreateDirectory(Path.Combine(root, id));
                File.WriteAllLines(Path.Combine(root, id, RunLog.UnityLogFile), new[] { "line 1 of " + id, "line 2 of " + id });
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Fact]
        public void Sessions_lists_the_newest_first_with_kind_and_time()
        {
            var runs = RunLog.Sessions(root);
            Assert.Equal("20261118-120000-cccccc", runs[0].Id);
            Assert.Equal("precheck", runs.Single(r => r.Id.StartsWith("precheck")).Kind);
            Assert.Equal(new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc), runs[0].StartedUtc);
        }

        [Fact]
        public void Mark_and_show_use_the_newest_editor_run()
        {
            var path = RunLog.Mark(root, null, "step A7");
            Assert.Equal(Path.Combine(root, "20261118-120000-cccccc", RunLog.WatchFile), path);
            var lines = RunLog.Show(root, null, 1);
            Assert.Equal(new[] { "line 2 of 20261118-120000-cccccc" }, lines.UnityLog);
            Assert.Contains("step A7", Assert.Single(lines.WatchLog));
        }

        [Fact]
        public void Prune_keeps_the_newest_folders()
        {
            RunLog.Prune(root, 2);
            Assert.Equal(new[] { "20261118-120000-cccccc", "precheck-20261118-115900" }, RunLog.Sessions(root).Select(r => r.Id));
        }

        [Fact]
        public void Show_of_an_unknown_run_fails()
            => Assert.Throws<FileNotFoundException>(() => RunLog.Show(root, "nope", 10));
    }
}
