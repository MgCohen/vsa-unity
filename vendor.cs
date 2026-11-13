// Copies the VSA packages and their dependencies into Packages/com.vsa.core/Runtime/Plugins. Unity cannot restore
// NuGet packages, so git keeps the DLLs. Run this script again after you change VsaVersion in Vendor/VsaVendor.csproj.
// The script also writes VsaVersion to the package version, and copies the VSA rule values for the editor script.
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
var output = Path.Combine(Path.GetTempPath(), $"vsa-vendor-{Guid.NewGuid():N}");
var project = Path.Combine(root, "Vendor", "VsaVendor.csproj");
var version = Regex.Match(File.ReadAllText(project), "<VsaVersion>(.+)</VsaVersion>").Groups[1].Value;

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
    .Where(path => Path.GetFileName(path) != "VsaVendor.dll" && !unity.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
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

// The rule values of VSA.Sdk (PackageDownload). The ".txt" name stops Unity from reading the file as a .globalconfig.
var packages = GlobalPackages();
var rules = Path.Combine(packages, "vsa.sdk", version.ToLowerInvariant(), "Sdk", "VSA.Rules.globalconfig");
File.WriteAllBytes(Path.Combine(package, "Editor", "VSA.Rules.globalconfig.txt"), File.ReadAllBytes(rules));

// The analyzers (PackageDownload). In the folder of the VSA.Unity asmdef, they run on VSA.Unity and on each assembly
// that references it. The label RoslynAnalyzer in the .meta file makes Unity use the DLL as an analyzer, not as a library.
var analyzers = Path.Combine(packages, "vsa.analyzers", version.ToLowerInvariant(), "analyzers", "dotnet", "cs", "VSA.Analyzers.dll");
var analyzer = Path.Combine(package, "Runtime", "VSA.Analyzers.dll");
File.WriteAllBytes(analyzer, File.ReadAllBytes(analyzers));
if (!File.Exists(analyzer + ".meta")) {
    File.WriteAllText(analyzer + ".meta", AnalyzerMeta.Replace("\r\n", "\n"));
}

var manifest = Path.Combine(package, "package.json");
File.WriteAllText(manifest, Regex.Replace(File.ReadAllText(manifest), "\"version\": \"[^\"]*\"", $"\"version\": \"{version}\""));

Console.WriteLine($"Packages/com.vsa.core {version}: {dlls.Count} DLLs.");
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
        guid: 7c0a5e1d3b9f4e2a8d6c1b0f9e8a7d61
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
