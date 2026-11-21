using System.Text.Json;

namespace VSA.TestSuite.Cli
{
    /// <summary>The command line: verbs, options and the configuration values.
    /// Each value comes from the first source that has it: option, environment variable, .vsa/vsa-unity.json, default.</summary>
    sealed class Options
    {
        readonly string[] args;
        readonly Dictionary<string, (string Value, string Source)> config = new();
        JsonElement? file;

        public Options(string[] args) => this.args = args;

        public string[] Verbs => args.TakeWhile(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();

        public bool Has(string name) => Array.IndexOf(args, name) >= 0;

        public string Get(string name, string fallback = null)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        /// <summary>An integer option. Throws ArgumentException for a value that is not a number.</summary>
        public int Int(string name, int fallback)
        {
            var text = Get(name);
            if (text == null) return fallback;
            if (!int.TryParse(text, out var value)) throw new ArgumentException(name + " needs a number, not '" + text + "'.");
            return value;
        }

        /// <summary>The Unity project: --project-path, or the first folder up from the current folder with ProjectSettings/ProjectVersion.txt.</summary>
        public string ProjectPath()
        {
            var start = Get("--project-path", Directory.GetCurrentDirectory());
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt"))) return dir.FullName;
            throw new DirectoryNotFoundException("No Unity project at or above " + start + ".");
        }

        /// <summary>A configuration value and its source.</summary>
        public (string Value, string Source) Config(string project, string option, string variable, string key, string fallback)
        {
            if (config.TryGetValue(key, out var known)) return known;
            (string, string) found;
            if (Get(option) is string o) found = (o, "option " + option);
            else if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } e) found = (e, "environment " + variable);
            else if (ReadFile(project) is JsonElement f && f.TryGetProperty(key, out var v)) found = (v.ToString(), ".vsa/vsa-unity.json");
            else found = (fallback, "default");
            config[key] = found;
            return found;
        }

        JsonElement? ReadFile(string project)
        {
            if (file != null || project == null) return file;
            var path = Path.Combine(project, ".vsa", "vsa-unity.json");
            if (System.IO.File.Exists(path)) file = JsonDocument.Parse(System.IO.File.ReadAllText(path)).RootElement.Clone();
            return file;
        }
    }
}
