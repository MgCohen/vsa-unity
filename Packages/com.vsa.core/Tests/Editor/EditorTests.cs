using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using VSA.Composition;

namespace VSA.Unity.Editor.Tests {
    public sealed class OneApp : VsaApp<OneApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).Modules();
    }

    public sealed class TwoApp : VsaApp<TwoApp> {
    }

    public sealed class Outer {
        public sealed class NestedApp : VsaApp<NestedApp> {
        }
    }

    public sealed class AppSearchTests {
        static readonly System.Reflection.Assembly[] TestAssembly = { typeof(OneApp).Assembly };

        [Test]
        public void No_app_class_gives_the_default_app() =>
            Assert.That(Apps.Find("", new[] { typeof(UnityEngine.Object).Assembly }), Is.EqualTo(typeof(DefaultVsaApp)));

        [Test]
        public void The_name_selects_the_app_class() =>
            Assert.That(Apps.Find(typeof(TwoApp).FullName, TestAssembly), Is.EqualTo(typeof(TwoApp)));

        [Test]
        public void More_than_one_app_class_and_no_name_stop_the_start() {
            var error = Assert.Throws<InvalidOperationException>(() => Apps.Find("", TestAssembly));

            Assert.That(error.Message, Does.Contain("3 app classes").And.Contain(typeof(OneApp).FullName));
        }

        [Test]
        public void An_unknown_name_stops_the_start() {
            var error = Assert.Throws<InvalidOperationException>(() => Apps.Find("Not.An.App", TestAssembly));

            Assert.That(error.Message, Does.Contain("no app class Not.An.App"));
        }

        [Test]
        public void The_app_scan_skips_the_test_assemblies() =>
            Assert.That(Apps.Assemblies(), Has.No.Member(typeof(OneApp).Assembly));
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
        public void A_new_csc_rsp_has_only_the_language_version() =>
            Assert.That(CompilerFiles.Rsp(""), Is.EqualTo("-langversion:12\n"));

        [Test]
        public void The_csc_rsp_keeps_the_other_lines_and_replaces_the_language_version() =>
            Assert.That(CompilerFiles.Rsp("-nowarn:0618\r\n-langversion:9\r\n-define:GAME\r\n"),
                Is.EqualTo("-langversion:12\n-nowarn:0618\n-define:GAME\n"));

        [Test]
        public void The_csc_rsp_does_not_change_two_times() {
            var once = CompilerFiles.Rsp("-define:GAME\n");

            Assert.That(CompilerFiles.Rsp(once), Is.EqualTo(once));
        }

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
            Assert.That(System.IO.File.ReadAllText(rsp[0]), Does.Contain("-analyzer:\"" + Analyzer + "\""));
        }

        [Test]
        public void The_package_has_the_rule_values_for_the_editor_script() {
            var text = CompilerFiles.GlobalConfig("", CompilerFiles.RulesText());

            Assert.That(text, Does.Contain("vsa.VSA1008.max = ").And.Contain("vsa.VSA1012.kind_order = "));
        }
    }

    public sealed class LinkerTests {
        const string Package = "<linker><assembly fullname=\"VSA.Analyzers.Markers\" preserve=\"all\" /></linker>";

        [Test]
        public void The_link_xml_keeps_the_markers_and_each_app_class() {
            var xml = VsaLinker.LinkXml(Package, new[] { typeof(OneApp), typeof(Outer.NestedApp) }).ToString();

            Assert.That(xml, Does.Contain("VSA.Analyzers.Markers"));
            Assert.That(xml, Does.Contain($"<assembly fullname=\"{typeof(OneApp).Assembly.GetName().Name}\">"));
            Assert.That(xml, Does.Contain("VSA.Unity.Editor.Tests.OneApp"));
            Assert.That(xml, Does.Contain("VSA.Unity.Editor.Tests.Outer/NestedApp"));
        }
    }
}
