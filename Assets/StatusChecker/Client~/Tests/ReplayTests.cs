using VSA.StatusChecker;
using Xunit;

namespace VSA.StatusChecker.Tests
{
    /// <summary>Replays the recorded live runs of the status lab through the current rules.</summary>
    public class ReplayTests
    {
        /// <summary>The scored windows of Tools/StatusLab/results/final.</summary>
        const int Scored = 117;

        /// <summary>2 windows fail: their trigger was lost before a lab fix (T10 run 1, T02 run 3).</summary>
        const int MinimumPass = 115;

        [Fact]
        public void The_recorded_runs_pass_115_of_117_or_better()
        {
            var lab = FindLab();
            var replayer = new Replayer();
            replayer.LoadOverrides(Path.Combine(lab, "truth-overrides.json"));
            var scored = replayer.ReplayFolder(Path.Combine(lab, "results", "final")).Where(r => r.Scored).ToList();
            var fails = scored.Where(r => !r.Pass).Select(r => r.File + " " + r.Case + ": " + r.Why).ToList();
            Assert.Equal(Scored, scored.Count);
            Assert.True(scored.Count(r => r.Pass) >= MinimumPass, "Fails:\n" + string.Join("\n", fails));
        }

        static string FindLab()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var lab = Path.Combine(dir.FullName, "Tools", "StatusLab");
                if (Directory.Exists(Path.Combine(lab, "results", "final"))) return lab;
            }
            throw new DirectoryNotFoundException("No Tools/StatusLab/results/final above " + AppContext.BaseDirectory);
        }
    }
}
