using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using VSA.Composition;

namespace VSA.Unity.Editor.Tests {
    public sealed class OneApp : VsaApp<OneApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).Modules();
    }

    public sealed class UnityHostTests {
        [TearDown]
        public void StopTheApp() => OneApp.Stop();

        [Test]
        public void Run_returns_while_the_app_runs() {
            Assert.That(OneApp.Run(Array.Empty<string>()), Is.Zero);

            Assert.That(OneApp.IsRunning, Is.True);
            Assert.That(VsaHost.IsRunning, Is.True);
        }

        [Test]
        public void The_exit_of_the_play_mode_stops_the_app() {
            OneApp.Run(Array.Empty<string>());

            UnityVsaHost.OnPlayMode(PlayModeStateChange.ExitingPlayMode);

            Assert.That(OneApp.IsRunning, Is.False);
            Assert.That(VsaHost.IsRunning, Is.False);
        }

        [Test]
        public void The_start_of_the_play_mode_does_not_stop_the_app() {
            OneApp.Run(Array.Empty<string>());

            UnityVsaHost.OnPlayMode(PlayModeStateChange.ExitingEditMode);

            Assert.That(OneApp.IsRunning, Is.True);
        }

        [Test]
        public void Run_after_Stop_starts_the_app_again() {
            OneApp.Run(Array.Empty<string>());
            OneApp.Stop();

            OneApp.Run(Array.Empty<string>());

            Assert.That(OneApp.IsRunning, Is.True);
        }
    }

    public sealed class CompilerFilesTests {
        const string Rules = "# rules\nis_global = true\nglobal_level = -1\n\nvsa.VSA1008.max = 120\n";

        [Test]
        public void A_new_globalconfig_has_the_VSA_block() =>
            Assert.That(CompilerFiles.GlobalConfig("", Rules), Is.EqualTo(
                "is_global = true\n" + CompilerFiles.Begin + "\n# rules\n\nvsa.VSA1008.max = 120\n" + CompilerFiles.End + "\n"));

        [Test]
        public void The_globalconfig_keeps_the_lines_of_the_consumer() {
            var text = CompilerFiles.GlobalConfig("is_global = true\ndotnet_diagnostic.CS0618.severity = none\n", Rules);

            Assert.That(text, Does.StartWith("is_global = true\ndotnet_diagnostic.CS0618.severity = none\n\n" + CompilerFiles.Begin));
            Assert.That(text.Split('\n').Count(line => line.StartsWith("is_global")), Is.EqualTo(1));
        }

        [Test]
        public void The_globalconfig_replaces_the_old_VSA_block() {
            var old = CompilerFiles.GlobalConfig("", "vsa.VSA1008.max = 80\n");

            var text = CompilerFiles.GlobalConfig(old, Rules);

            Assert.That(text, Does.Contain("vsa.VSA1008.max = 120").And.Not.Contain("= 80"));
            Assert.That(CompilerFiles.GlobalConfig(text, Rules), Is.EqualTo(text));
        }
    }

    public sealed class AnalyzerTests {
        const string Analyzer = "Packages/com.vsa.core/Runtime/VSA.Analyzers.dll";

        [Test]
        public void The_analyzer_DLL_has_the_RoslynAnalyzer_label() =>
            Assert.That(AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(Analyzer)), Does.Contain("RoslynAnalyzer"));

        [Test]
        public void The_analyzer_DLL_is_not_in_a_player_or_the_editor() {
            var plugin = (PluginImporter)AssetImporter.GetAtPath(Analyzer);

            Assert.That(plugin.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(plugin.GetCompatibleWithEditor(), Is.False);
        }

        [Test]
        public void The_analyzers_run_on_an_assembly_that_references_VSA_Unity() {
            var rsp = System.IO.Directory.GetFiles("Library/Bee/artifacts", "VSA.Unity.Editor.Tests.rsp", System.IO.SearchOption.AllDirectories);

            Assert.That(rsp, Is.Not.Empty);
            Assert.That(System.IO.File.ReadAllText(rsp[0]), Does.Match("-analyzer:\"[^\"]*com\\.vsa\\.core[^\"]*/Runtime/VSA\\.Analyzers\\.dll\""));
        }

        [Test]
        public void The_shipped_config_is_the_text_that_the_editor_script_writes_in_a_new_project() =>
            Assert.That(CompilerFiles.GlobalConfig("", CompilerFiles.RulesText()),
                Is.EqualTo(System.IO.File.ReadAllText(CompilerFiles.ShippedConfigPath()).Replace("\r\n", "\n")));

        [Test]
        public void The_package_has_the_rule_values_for_the_editor_script() {
            var text = CompilerFiles.GlobalConfig("", CompilerFiles.RulesText());

            Assert.That(text, Does.Contain("vsa.VSA1008.max = ").And.Contain("vsa.VSA1012.kind_order = "));
        }
    }
}
