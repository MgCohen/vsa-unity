# Runs the Unity tests of the package (Unity Test Framework, batch mode): EditMode, then PlayMode.
#
#   ./run-tests.ps1 [-Unity <path of Unity.exe>]
#
# The results are in TestResults-EditMode.xml and TestResults-PlayMode.xml. Exit code 0: all tests passed.
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$failed = 0
foreach ($platform in "EditMode", "PlayMode") {
    $results = Join-Path $root "TestResults-$platform.xml"
    $log = Join-Path $root "Logs/tests-$platform.log"
    & $Unity -batchmode -nographics -projectPath $root -runTests -testPlatform $platform -testResults $results -logFile $log | Out-Null
    $code = $LASTEXITCODE
    if (-not (Test-Path $results)) {
        Write-Error "$platform`: no test results (exit code $code). See $log."
    }
    $run = ([xml](Get-Content $results)).'test-run'
    "$platform`: $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped of $($run.total)"
    ([xml](Get-Content $results)).SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
        "  FAIL $($_.fullname): $($_.failure.message.'#cdata-section')"
    }
    if ($code -ne 0) { $failed++ }
}
exit $failed
