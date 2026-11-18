# Runs the probe checks.
#
#   ./probe.ps1 editor                                  the checks in the Editor (Mono)
#   ./probe.ps1 player [-Backend IL2CPP|Mono2x] [-Stripping Minimal] [-CodeGeneration OptimizeSpeed]
#                                                       builds the Windows player, then runs the checks in it
#
# The CHECK lines go to the console. The logs go to Results/.
param(
    [Parameter(Mandatory)][ValidateSet("editor", "player")][string]$Mode,
    [string]$Backend = "IL2CPP",
    [string]$Stripping = "Minimal",
    [string]$CodeGeneration = "OptimizeSpeed",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$results = Join-Path $root "Results"
New-Item -ItemType Directory -Force $results | Out-Null

function Checks($log) {
    Select-String -Path $log -Pattern "^CHECK " | ForEach-Object { $_.Line } | Select-Object -Unique
}

if ($Mode -eq "editor") {
    $log = Join-Path $results "editor.log"
    & $Unity -batchmode -nographics -projectPath $root -executeMethod Probe.Editor.ProbeEditor.Run -logFile $log | Out-Null
    $code = $LASTEXITCODE
    Checks $log
    Select-String -Path $log -Pattern "error CS" | ForEach-Object { $_.Line }
    exit $code
}

$name = "$Backend-$Stripping-$CodeGeneration"
$player = Join-Path $root "Builds\$name"
$build = Join-Path $results "build-$name.log"
& $Unity -batchmode -nographics -projectPath $root -executeMethod Probe.Editor.ProbeEditor.Build `
    -probeOutput $player -probeBackend $Backend -probeStripping $Stripping -probeCodeGeneration $CodeGeneration -logFile $build | Out-Null
$built = $LASTEXITCODE
Checks $build
if ($built -ne 0) {
    Write-Error "The build failed (exit code $built). See $build."
}
$log = Join-Path $results "player-$name.log"
$run = Start-Process -FilePath (Join-Path $player "Probe.exe") -ArgumentList "-batchmode", "-nographics", "-logFile", "`"$log`"" -PassThru
if (-not $run.WaitForExit(120000)) {
    Stop-Process -Id $run.Id -Force
    Write-Error "The player did not stop in 2 minutes. See $log."
}
Checks $log
"player exit code $($run.ExitCode)"
