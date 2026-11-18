using System.Text.Json;
using VSA.TestSuite.Client.Status;
using Xunit;

namespace VSA.TestSuite.Client.Tests
{
    public class RuleTests
    {
        [Fact]
        public void States_all_has_26_unique_states_and_ends_with_the_frozen_fallback()
        {
            var names = States.All.Select(s => s.Name).ToList();
            Assert.Equal(26, names.Count);
            Assert.Equal(names.Count, names.Distinct().Count());
            Assert.Equal("frozen", names[^1]);
            Assert.True(names.IndexOf("native-dialog") > names.IndexOf("backup-prompt"));
            Assert.True(names.IndexOf("ready-idle") > names.IndexOf("play-running"));
        }

        [Fact]
        public void Each_limit_is_for_a_known_state()
        {
            using var doc = JsonDocument.Parse(Limits.ReadDefault());
            var names = States.All.Select(s => s.Name).Append("cold-start-full").ToHashSet();
            foreach (var p in doc.RootElement.GetProperty("states").EnumerateObject())
                Assert.Contains(p.Name, names);
        }

        [Theory]
        [InlineData("compile-failed", StateAction.Fail, null, "CompileFailed")]
        [InlineData("safe-mode", StateAction.Fail, null, "SafeMode")]
        [InlineData("native-dialog", StateAction.Answer, null, "Dialog")]
        [InlineData("frozen", StateAction.Escalate, "stalled", "Stalled")]
        [InlineData("domain-reload", StateAction.Escalate, "stuck", "Stuck")]
        [InlineData("ready-idle", StateAction.Fail, "loop", "Loop")]
        [InlineData("crash", StateAction.Start, null, "Crash")]
        [InlineData("no-editor", StateAction.Start, null, "NoEditor")]
        [InlineData("exited", StateAction.None, null, "NoEditor")]
        [InlineData("frozen", StateAction.Wait, null, null)]
        [InlineData("domain-reload", StateAction.Wait, null, null)]
        [InlineData("server-down", StateAction.Escalate, null, null)]
        public void A_wait_stops_with_the_code_of_the_state(string state, StateAction action, string result, string code)
            => Assert.Equal(code, WaitRules.StopCode(new StateResult { State = state, Action = action, Result = result }));

        [Fact]
        public void An_attached_wait_does_not_stop_for_no_editor()
            => Assert.Null(WaitRules.StopCode(new StateResult { State = "no-editor", Action = StateAction.Start }, editorRequired: false));

        [Fact]
        public void Until_ready_means_the_three_ready_states()
        {
            Assert.Equal(new HashSet<string> { "ready-idle", "play-running", "play-paused" }, WaitRules.ParseUntil("ready"));
            Assert.Equal(new HashSet<string> { "frozen", "exited" }, WaitRules.ParseUntil("frozen, exited"));
            Assert.Throws<ArgumentException>(() => WaitRules.ParseUntil("sleeping"));
        }

        [Fact]
        public void The_busy_window_is_found_by_its_structure_in_any_language()
        {
            Assert.True(WindowSource.IsBusyWindow("#32770", new[] { "Button", "Button", "msctls_progress32", "Static" }));
            Assert.True(WindowSource.IsBusyWindow("#32770", new[] { "msctls_progress32" }));
            Assert.False(WindowSource.IsBusyWindow("#32770", new[] { "Button", "Static" }));
            Assert.False(WindowSource.IsBusyWindow("UnitySplashWindow", new[] { "msctls_progress32" }));
        }

        [Theory]
        [InlineData("Hold on (busy for 31s)...", 31)]
        [InlineData("Running managed callbacks (busy for 01:01)...", 61)]
        [InlineData("Bitte warten (beschäftigt seit 12s)...", 12)]
        [InlineData("Importing", null)]
        public void The_busy_time_comes_from_the_number_in_the_title(string title, int? seconds)
            => Assert.Equal(seconds, WindowSource.BusySeconds(title));
    }
}
