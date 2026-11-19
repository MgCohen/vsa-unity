// Copies the Umpire packages and their dependencies into Packages/com.vsa.core/Runtime/Plugins. Unity cannot restore
// NuGet packages, so git keeps the DLLs. Run this script again after you change UmpireVersion in Vendor/UmpireVendor.csproj.
// The script also writes UmpireVersion to the package version, and copies the analyzers and their rule values.
//
//   dotnet run --file vendor.cs
//
// GitHub Packages needs a token: set GITHUB_TOKEN, for example to the output of `gh auth token`.
// Each argument goes to dotnet publish, for example --no-http-cache after a release, or
// -p:RestoreAdditionalProjectSources=<local feed>. "--file" is necessary, because Unity writes .csproj files here.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

var root = Path.GetDirectoryName(Here())!;
var package = Path.Combine(root, "Packages", "com.vsa.core");
var plugins = Path.Combine(package, "Runtime", "Plugins");
var output = Path.Combine(Path.GetTempPath(), $"umpire-vendor-{Guid.NewGuid():N}");
var project = Path.Combine(root, "Vendor", "UmpireVendor.csproj");
var version = Regex.Match(File.ReadAllText(project), "<UmpireVersion>(.+)</UmpireVersion>").Groups[1].Value;

var publish = new ProcessStartInfo("dotnet") { WorkingDirectory = root };
foreach (var arg in (string[])["publish", project, "-c", "Release", "-o", output, .. args]) {
    publish.ArgumentList.Add(arg);
}
using (var process = Process.Start(publish)!) {
    process.WaitForExit();
    if (process.ExitCode != 0) {
        Console.Error.WriteLine("dotnet publish failed. If the error is 401, set GITHUB_TOKEN.");
        return process.ExitCode;
    }
}

// Unity 6 ships these DLLs (Editor/Data/BCLExtensions/runtime/netstandard2.1). A second copy is a compile error.
string[] unity = [
    "Microsoft.Bcl.AsyncInterfaces.dll", "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
    "Microsoft.Extensions.Logging.Abstractions.dll", "System.Collections.Immutable.dll", "System.Diagnostics.DiagnosticSource.dll",
    "System.IO.Hashing.dll", "System.Reflection.Metadata.dll", "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Text.Encodings.Web.dll", "System.Text.Json.dll",
];

// The project itself does not ship. Each other DLL goes to the plugins folder.
var dlls = Directory.GetFiles(output, "*.dll")
    .Where(path => Path.GetFileName(path) != "UmpireVendor.dll" && !unity.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
    .ToDictionary(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

Directory.CreateDirectory(plugins);
// A DLL that is not in the output goes, with its .meta file. A DLL that stays keeps its .meta file, so its GUID stays the same.
foreach (var old in Directory.GetFiles(plugins, "*.dll").Where(path => !dlls.ContainsKey(Path.GetFileName(path)))) {
    File.Delete(old);
    File.Delete(old + ".meta");
}
// Write the bytes, not File.Copy: File.Copy also copies the file mode of the NuGet cache, and git sees a change.
foreach (var (name, path) in dlls) {
    File.WriteAllBytes(Path.Combine(plugins, name), File.ReadAllBytes(path));
}
Directory.Delete(output, recursive: true);

// The analyzers and their rule values (PackageDownload). In the folder of the VSA.Unity asmdef, the analyzers run on
// VSA.Unity and on each assembly that references it. The label RoslynAnalyzer in the .meta file makes Unity use
// the DLL as an analyzer, not as a library.
var analyzerVersion = Regex.Match(File.ReadAllText(project), "<UmpireAnalyzerVersion>(.+)</UmpireAnalyzerVersion>") is { Success: true } pin
    ? pin.Groups[1].Value
    : version;
var analyzerPackage = Path.Combine(GlobalPackages(), "umpire.vsa.analyzers", analyzerVersion.ToLowerInvariant());
string[] analyzerNames = ["Umpire.Kernels.Analyzers", "Umpire.Vsa.Analyzers"];
foreach (var old in Directory.GetFiles(Path.Combine(package, "Runtime"), "*.Analyzers.dll")
    .Where(path => !analyzerNames.Contains(Path.GetFileNameWithoutExtension(path)))) {
    File.Delete(old);
    File.Delete(old + ".meta");
}
foreach (var name in analyzerNames) {
    var analyzer = Path.Combine(package, "Runtime", name + ".dll");
    File.WriteAllBytes(analyzer, File.ReadAllBytes(Path.Combine(analyzerPackage, "analyzers", "dotnet", "cs", name + ".dll")));
    if (!File.Exists(analyzer + ".meta")) {
        File.WriteAllText(analyzer + ".meta", AnalyzerMeta.Replace("{guid}", Guid.NewGuid().ToString("N")).Replace("\r\n", "\n"));
    }
}

// The rule values of the analyzers, as the file that a consumer copies to Assets/Default.globalconfig. Unity reads
// no globalconfig from a package. Unity does not import a folder that ends with "~". The editor script CompilerFiles
// reads the block of this file, and an EditMode test checks that the file is the same as the text that CompilerFiles writes.
var rules = analyzerNames
    .SelectMany(name => File.ReadAllText(Path.Combine(analyzerPackage, "build", name + ".globalconfig"))
        .Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
        .Where(line => !Regex.IsMatch(line, @"^\s*(is_global|global_level)\s*="))
        .SkipWhile(line => line.Trim().Length == 0)
        .Append(""))
    .SkipLast(1);
string[] config = ["is_global = true", "# BEGIN com.vsa.core: the editor script of the package writes these lines.", .. rules,
    "# END com.vsa.core"];
Directory.CreateDirectory(Path.Combine(package, "Documentation~"));
File.WriteAllText(Path.Combine(package, "Documentation~", "Default.globalconfig"), string.Join("\n", config) + "\n");

var manifest = Path.Combine(package, "package.json");
File.WriteAllText(manifest, Regex.Replace(File.ReadAllText(manifest), "\"version\": \"[^\"]*\"", $"\"version\": \"{version}\""));

Console.WriteLine($"Packages/com.vsa.core {version}: {dlls.Count} DLLs. Analyzers: {analyzerVersion}.");
return 0;

static string GlobalPackages() {
    var list = new ProcessStartInfo("dotnet", "nuget locals global-packages --list") { RedirectStandardOutput = true };
    using var process = Process.Start(list)!;
    var line = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    return line[(line.IndexOf(':') + 1)..].Trim();
}

static string Here([CallerFilePath] string path = "") => path;

static partial class Program {
    // No platform: Unity does not reference the DLL and does not put it in a player.
    const string AnalyzerMeta = """
        fileFormatVersion: 2
        guid: {guid}
        labels:
        - RoslynAnalyzer
        PluginImporter:
          externalObjects: {}
          serializedVersion: 2
          iconMap: {}
          executionOrder: {}
          defineConstraints: []
          isPreloaded: 0
          isOverridable: 0
          isExplicitlyReferenced: 0
          validateReferences: 1
          platformData:
          - first:
              Any:
            second:
              enabled: 0
              settings: {}
          - first:
              Editor: Editor
            second:
              enabled: 0
              settings:
                DefaultValueInitialized: true
          userData:
          assetBundleName:
          assetBundleVariant:

        """;
}
