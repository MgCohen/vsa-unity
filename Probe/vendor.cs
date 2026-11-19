// Copies the Umpire packages and their dependencies into Assets/Plugins/Umpire. Unity cannot restore NuGet packages.
//
//   dotnet run --file vendor.cs
//
// GitHub Packages needs a token: set GITHUB_TOKEN, for example to the output of `gh auth token`.

using System.Diagnostics;
using System.Runtime.CompilerServices;

var root = Path.GetDirectoryName(Here())!;
var plugins = Path.Combine(root, "Assets", "Plugins", "Umpire");
var output = Path.Combine(Path.GetTempPath(), $"umpire-vendor-{Guid.NewGuid():N}");
var project = Path.Combine(root, "Vendor", "UmpireVendor.csproj");

var publish = new ProcessStartInfo("dotnet") { WorkingDirectory = Path.Combine(root, "..") };
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

var dlls = Directory.GetFiles(output, "*.dll")
    .Where(path => Path.GetFileName(path) != "UmpireVendor.dll" && !unity.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
    .ToDictionary(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

Directory.CreateDirectory(plugins);
foreach (var old in Directory.GetFiles(plugins, "*.dll").Where(path => !dlls.ContainsKey(Path.GetFileName(path)))) {
    File.Delete(old);
    File.Delete(old + ".meta");
}
foreach (var (name, path) in dlls) {
    File.WriteAllBytes(Path.Combine(plugins, name), File.ReadAllBytes(path));
}
Directory.Delete(output, recursive: true);
Console.WriteLine($"Assets/Plugins/Umpire: {dlls.Count} DLLs.");
foreach (var name in dlls.Keys.Order()) {
    Console.WriteLine(name);
}
return 0;

static string Here([CallerFilePath] string path = "") => path;
