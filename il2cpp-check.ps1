# The IL2CPP player check of the package (dev machine). It builds the Windows IL2CPP player of the sample
# (managed stripping Minimal, or Low with -Stripping Low), runs it, and reads the CHECK lines of the player log.
# For a fast linker check, -Backend Mono2x builds a Mono player: the Unity linker also runs for Mono.
#
#   ./il2cpp-check.ps1 [-Unity <path of Unity.exe>] [-Stripping Minimal|Low] [-Backend IL2CPP|Mono2x]
#
# Exit code 0: the build succeeded, and the player wrote "CHECK done ok" and no FAIL line.
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe",
    [ValidateSet("Minimal", "Low")]
    [string]$Stripping = "Minimal",
    [ValidateSet("IL2CPP", "Mono2x")]
    [string]$Backend = "IL2CPP"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$out = Join-Path $root "Il2cppCheck"
$player = Join-Path $out "Player"
if (Test-Path $player) { Remove-Item -Recurse -Force $player }
New-Item -ItemType Directory -Force $out | Out-Null

$build = Join-Path $out "build.log"
& $Unity -batchmode -nographics -projectPath $root -executeMethod Il2cppCheck.Build -checkOutput $player -checkStripping $Stripping -checkBackend $Backend -logFile $build | Out-Null
$built = $LASTEXITCODE
Select-String -Path $build -Pattern "^CHECK " | ForEach-Object { $_.Line }
if ($built -ne 0) {
    Write-Error "The IL2CPP build failed (exit code $built). See $build."
}

$log = Join-Path $out "player.log"
$run = Start-Process -FilePath (Join-Path $player "VsaCheck.exe") -ArgumentList "-batchmode", "-nographics", "-logFile", "`"$log`"", "--Probe:Value=42", "--Probe:Order=process" -PassThru
# The player quits by itself. If it does not stop in 2 minutes, the check fails.
if (-not $run.WaitForExit(120000)) {
    Stop-Process -Id $run.Id -Force
    Write-Error "The player did not stop in 2 minutes. See $log."
}
$lines = @(Select-String -Path $log -Pattern "^CHECK " | ForEach-Object { $_.Line })
$lines
if ($run.ExitCode -ne 0 -or -not ($lines -contains "CHECK done ok") -or ($lines -match " FAIL ")) {
    Write-Error "The IL2CPP check failed (player exit code $($run.ExitCode)). See $log."
}
"IL2CPP check: ok"
