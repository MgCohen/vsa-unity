using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Umpire.Kernels;
using Umpire.Vsa;
using UnityEditor;

namespace Umpire.Unity.Editor.Tests {
    // An app over the Unity host with no modules. The module scan does not read a test assembly.
    public sealed class EmptyHost : UnityHost {
        public EmptyHost() : base(Array.Empty<Module>()) {
        }
    }

    public sealed class EmptyApp : App<Vsa<EmptyHost>> {
    }

    public sealed class UnityHostTests {
        [Test]
        public void An_app_over_the_Unity_host_starts_and_stops_in_Edit_Mode() {
            var app = new EmptyApp();

            app.Start(Array.Empty<string>());
            Assert.That(app.IsRunning, Is.True);
            app.Stop();

            Assert.That(app.IsRunning, Is.False);
        }
    }

    public sealed class CompilerFilesTests {
        const string Rules = "# rules\nis_global = true\nglobal_level = -1\n\numpire.UMP010101.max = 120\n";

        [Test]
        public void A_new_globalconfig_has_the_package_block() =>
            Assert.That(CompilerFiles.GlobalConfig("", Rules), Is.EqualTo(
                "is_global = true\n" + CompilerFiles.Begin + "\n# rules\n\numpire.UMP010101.max = 120\n" + CompilerFiles.End + "\n"));

        [Test]
        public void The_globalconfig_keeps_the_lines_of_the_consumer() {
            var text = CompilerFiles.GlobalConfig("is_global = true\ndotnet_diagnostic.CS0618.severity = none\n", Rules);

            Assert.That(text, Does.StartWith("is_global = true\ndotnet_diagnostic.CS0618.severity = none\n\n" + CompilerFiles.Begin));
            Assert.That(text.Split('\n').Count(line => line.StartsWith("is_global")), Is.EqualTo(1));
        }

        [Test]
        public void The_globalconfig_replaces_the_last_package_block() {
            var old = CompilerFiles.GlobalConfig("", "umpire.UMP010101.max = 80\n");

            var text = CompilerFiles.GlobalConfig(old, Rules);

            Assert.That(text, Does.Contain("umpire.UMP010101.max = 120").And.Not.Contain("= 80"));
            Assert.That(CompilerFiles.GlobalConfig(text, Rules), Is.EqualTo(text));
        }
    }

    public sealed class AnalyzerTests {
        static readonly string[] Analyzers = {
            "Packages/com.vsa.core/Runtime/Umpire.Kernels.Analyzers.dll",
            "Packages/com.vsa.core/Runtime/Umpire.Vsa.Analyzers.dll",
        };

        [TestCaseSource(nameof(Analyzers))]
        public void The_analyzer_DLL_has_the_RoslynAnalyzer_label(string analyzer) =>
            Assert.That(AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(analyzer)), Does.Contain("RoslynAnalyzer"));

        [TestCaseSource(nameof(Analyzers))]
        public void The_analyzer_DLL_is_not_in_a_player_or_the_editor(string analyzer) {
            var plugin = (PluginImporter)AssetImporter.GetAtPath(analyzer);

            Assert.That(plugin.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(plugin.GetCompatibleWithEditor(), Is.False);
        }

        [TestCaseSource(nameof(Analyzers))]
        public void The_analyzers_run_on_an_assembly_that_references_Umpire_Unity(string analyzer) {
            var rsp = Directory.GetFiles("Library/Bee/artifacts", "Umpire.Unity.Editor.Tests.rsp", SearchOption.AllDirectories);
            var name = Path.GetFileName(analyzer).Replace(".", "\\.");

            Assert.That(rsp, Is.Not.Empty);
            Assert.That(File.ReadAllText(rsp[0]), Does.Match("-analyzer:\"[^\"]*com\\.vsa\\.core[^\"]*/Runtime/" + name + "\""));
        }

        [Test]
        public void The_shipped_config_is_the_text_that_the_editor_script_writes_in_a_new_project() =>
            Assert.That(CompilerFiles.GlobalConfig("", CompilerFiles.RulesText()),
                Is.EqualTo(File.ReadAllText(CompilerFiles.ShippedConfigPath()).Replace("\r\n", "\n")));

        [Test]
        public void The_package_has_the_rule_values_of_both_analyzers() {
            var text = CompilerFiles.GlobalConfig("", CompilerFiles.RulesText());

            Assert.That(text, Does.Contain("Umpire.Kernels.Analyzers").And.Contain("Umpire.Vsa.Analyzers"));
            Assert.That(text, Does.Contain("dotnet_diagnostic.UMP010203.severity = none"));
        }
    }
}
