using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace VSA.Unity.Editor {
    // Writes the VSA rule values to Assets/Default.globalconfig of the consumer project when the editor loads,
    // for the VSA analyzers. Unity reads only this name (all assemblies) or <AsmdefName>.globalconfig next to an asmdef.
    // The script changes only its own lines, and writes the file only if its text changes.
    [InitializeOnLoad]
    static class CompilerFiles {
        internal const string Begin = "# BEGIN com.vsa.core: the editor script of the package writes these lines.";
        internal const string End = "# END com.vsa.core";

        static CompilerFiles() => EditorApplication.delayCall += Write;

        [MenuItem("Tools/VSA/Write Compiler Files")]
        static void Write() {
            var rules = RulesText();
            if (rules != null && Change("Assets/Default.globalconfig", existing => GlobalConfig(existing, rules))) {
                AssetDatabase.Refresh();
            }
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

        // The rule values of the VSA version of the package: the block of Documentation~/Default.globalconfig,
        // the file that the install step copies to Assets/Default.globalconfig (vendor.cs writes it).
        internal static string RulesText() {
            var path = ShippedConfigPath();
            if (path == null || !File.Exists(path)) {
                return null;
            }
            var lines = Lines(File.ReadAllText(path)).ToList();
            var start = lines.IndexOf(Begin);
            var end = lines.IndexOf(End);
            return start >= 0 && end > start ? string.Join("\n", lines.Skip(start + 1).Take(end - start - 1)) + "\n" : null;
        }

        internal static string ShippedConfigPath() {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CompilerFiles).Assembly);
            return package == null ? null : Path.Combine(package.resolvedPath, "Documentation~", "Default.globalconfig");
        }

        static bool IsKey(string line, string key) {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith(key, StringComparison.Ordinal) && trimmed.Substring(key.Length).TrimStart().StartsWith("=", StringComparison.Ordinal);
        }

        static string[] Lines(string text) =>
            text.Length == 0 ? Array.Empty<string>() : text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
    }
}
