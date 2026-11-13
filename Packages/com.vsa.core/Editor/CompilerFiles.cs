using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace VSA.Unity.Editor {
    // Writes two compiler files of the consumer project when the editor loads:
    //   Assets/csc.rsp             -langversion:12 (Unity 6000.6 has Roslyn 4.10: C# 12 is the highest).
    //                              It applies to each assembly with no own csc.rsp, also the package assemblies.
    //   Assets/Default.globalconfig  the VSA rule values, for the VSA analyzers. Unity reads only this name
    //                              (all assemblies) or <AsmdefName>.globalconfig next to an asmdef.
    // The script changes only its own lines, and writes a file only if its text changes.
    [InitializeOnLoad]
    static class CompilerFiles {
        internal const string LangVersion = "-langversion:12";
        internal const string Begin = "# BEGIN com.vsa.core: the editor script of the package writes these lines.";
        internal const string End = "# END com.vsa.core";

        static CompilerFiles() => EditorApplication.delayCall += Write;

        [MenuItem("Tools/VSA/Write Compiler Files")]
        static void Write() {
            var rsp = Change("Assets/csc.rsp", Rsp);
            var rules = RulesText();
            var config = rules != null && Change("Assets/Default.globalconfig", existing => GlobalConfig(existing, rules));
            if (rsp || config) {
                AssetDatabase.Refresh();
            }
        }

        // One -langversion line, first. The other lines stay.
        internal static string Rsp(string existing) {
            var kept = Lines(existing).Where(line => !line.TrimStart().StartsWith("-langversion:", StringComparison.Ordinal));
            return string.Join("\n", new[] { LangVersion }.Concat(kept)).TrimEnd('\n') + "\n";
        }

        // The VSA block between Begin and End. The lines of the consumer stay. One is_global line, first.
        internal static string GlobalConfig(string existing, string rules) {
            var block = Lines(rules).Where(line => !IsKey(line, "is_global") && !IsKey(line, "global_level"));
            var lines = Lines(existing).ToList();
            var start = lines.IndexOf(Begin);
            var end = lines.IndexOf(End);
            if (start >= 0 && end > start) {
                lines.RemoveRange(start, end - start + 1);
            }
            var own = lines.Where(line => !IsKey(line, "is_global")).ToList();
            while (own.Count > 0 && own[own.Count - 1].Trim().Length == 0) {
                own.RemoveAt(own.Count - 1);
            }
            var text = new[] { "is_global = true" }
                .Concat(own)
                .Concat(own.Count > 0 ? new[] { "" } : Array.Empty<string>())
                .Concat(new[] { Begin })
                .Concat(block.SkipWhile(line => line.Trim().Length == 0))
                .Concat(new[] { End });
            return string.Join("\n", text) + "\n";
        }

        static bool Change(string path, Func<string, string> write) {
            var existing = File.Exists(path) ? File.ReadAllText(path) : "";
            var text = write(existing);
            if (text == existing.Replace("\r\n", "\n")) {
                return false;
            }
            File.WriteAllText(path, text);
            return true;
        }

        // The rule values of the VSA version of the package (vendor.cs copies them).
        static string RulesText() {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CompilerFiles).Assembly);
            var path = package == null ? null : Path.Combine(package.resolvedPath, "Editor", "VSA.Rules.globalconfig.txt");
            return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
        }

        static bool IsKey(string line, string key) {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith(key, StringComparison.Ordinal) && trimmed.Substring(key.Length).TrimStart().StartsWith("=", StringComparison.Ordinal);
        }

        static string[] Lines(string text) =>
            text.Length == 0 ? Array.Empty<string>() : text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
    }
}
