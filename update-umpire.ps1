# Updates the Umpire DLLs of the package from the release archive of vsa-template (umpire-<version>.zip).
# Unity cannot restore NuGet packages, so git keeps the DLLs. Run this script on a dev computer for each new release.
#
#   ./update-umpire.ps1 [-Version <version>] [-Archive <path of umpire-<version>.zip>]
#
# With no -Version and no -Archive, the script takes the newest template release (a preview release too).
# The script needs no token and no .NET SDK: the template repository is public.
# The script writes the version to package.json, and prints "version=<version>" as its last line.
param(
    [string]$Version,
    [string]$Archive
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$repository = "MgCohen/vsa-template"
$package = Join-Path $PSScriptRoot "Packages/com.vsa.core"
$runtime = Join-Path $package "Runtime"
$plugins = Join-Path $runtime "Plugins"

# Unity 6 ships these DLLs (Editor/Data/BCLExtensions/runtime/netstandard2.1). A second copy is a compile error.
$unityDlls = @(
    "Microsoft.Bcl.AsyncInterfaces.dll", "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
    "Microsoft.Extensions.Logging.Abstractions.dll", "System.Collections.Immutable.dll", "System.Diagnostics.DiagnosticSource.dll",
    "System.IO.Hashing.dll", "System.Reflection.Metadata.dll", "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Text.Encodings.Web.dll", "System.Text.Json.dll"
)

# The .meta file of a new analyzer DLL. No platform: Unity does not reference the DLL and does not put it in a player.
$AnalyzerMeta = (@"
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

"@).Replace("`r`n", "`n")

$work = Join-Path ([IO.Path]::GetTempPath()) ("umpire-update-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $work | Out-Null
try {
    if (-not $Archive) {
        if (-not $Version) {
            # The first item of the release list is the newest release. "releases/latest" skips preview releases.
            $headers = @{ "User-Agent" = "umpire-update" }
            if ($env:GITHUB_TOKEN) { $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN" }
            $releases = Invoke-RestMethod -Headers $headers -Uri "https://api.github.com/repos/$repository/releases?per_page=1"
            $Version = $releases[0].tag_name.TrimStart("v")
        }
        $Archive = Join-Path $work "umpire-$Version.zip"
        Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/$repository/releases/download/v$Version/umpire-$Version.zip" -OutFile $Archive
    }

    $files = Join-Path $work "archive"
    Expand-Archive -Path $Archive -DestinationPath $files
    $manifest = Get-Content -Raw (Join-Path $files "manifest.json") | ConvertFrom-Json
    if ($Version -and $manifest.version -ne $Version) {
        throw "The archive is version $($manifest.version), not $Version."
    }
    $Version = $manifest.version

    # Each file of the manifest must have its SHA-256 value. A changed or missing file stops the update.
    foreach ($entry in $manifest.files) {
        $path = Join-Path $files $entry.path
        if (-not (Test-Path $path)) { throw "The archive has no $($entry.path)." }
        $hash = (Get-FileHash -Algorithm SHA256 $path).Hash.ToLowerInvariant()
        if ($entry.sha256 -and $hash -ne $entry.sha256) { throw "The SHA-256 value of $($entry.path) is not correct." }
    }

    # The runtime DLLs go to Runtime/Plugins. A DLL that is not in the archive goes, with its .meta file.
    # A DLL that stays keeps its .meta file, so its GUID stays the same.
    $dlls = @(Get-ChildItem (Join-Path $files "runtime/netstandard2.1") -Filter *.dll | Where-Object { $unityDlls -notcontains $_.Name })
    New-Item -ItemType Directory -Force $plugins | Out-Null
    foreach ($old in Get-ChildItem $plugins -Filter *.dll) {
        if (($dlls | ForEach-Object Name) -notcontains $old.Name) {
            Remove-Item $old.FullName, ($old.FullName + ".meta") -ErrorAction SilentlyContinue
        }
    }
    foreach ($dll in $dlls) {
        $target = Join-Path $plugins $dll.Name
        # Write the bytes, not Copy-Item, so that git sees no change of the file mode.
        [IO.File]::WriteAllBytes($target, [IO.File]::ReadAllBytes($dll.FullName))
        if (-not (Test-Path ($target + ".meta"))) {
            # A package from a git URL is immutable, so Unity cannot write a .meta file. The package must have it.
            $meta = "fileFormatVersion: 2`nguid: " + [guid]::NewGuid().ToString("N") + "`n"
            [IO.File]::WriteAllText($target + ".meta", $meta)
        }
    }

    # The analyzers go to the folder of the Umpire.Unity asmdef. They run on Umpire.Unity and on each assembly that
    # references it. The label RoslynAnalyzer in the .meta file makes Unity use the DLL as an analyzer, not as a library.
    $analyzers = @(Get-ChildItem (Join-Path $files "analyzers/dotnet/cs") -Filter *.dll)
    foreach ($old in Get-ChildItem $runtime -Filter *.Analyzers.dll) {
        if (($analyzers | ForEach-Object Name) -notcontains $old.Name) {
            Remove-Item $old.FullName, ($old.FullName + ".meta") -ErrorAction SilentlyContinue
        }
    }
    foreach ($dll in $analyzers) {
        $target = Join-Path $runtime $dll.Name
        [IO.File]::WriteAllBytes($target, [IO.File]::ReadAllBytes($dll.FullName))
        if (-not (Test-Path ($target + ".meta"))) {
            [IO.File]::WriteAllText($target + ".meta", $AnalyzerMeta.Replace("{guid}", [guid]::NewGuid().ToString("N")))
        }
    }

    $json = Join-Path $package "package.json"
    $text = [IO.File]::ReadAllText($json) -replace '"version": "[^"]*"', ('"version": "' + $Version + '"')
    [IO.File]::WriteAllText($json, $text)

    "Packages/com.vsa.core: $($dlls.Count) runtime DLLs and $($analyzers.Count) analyzers of Umpire $Version (commit $($manifest.commit))."
    "version=$Version"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
