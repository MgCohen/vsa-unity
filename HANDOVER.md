# Handover: Umpire probe in Unity

The folder `Probe/` is a separate Unity project with a minimal Umpire `0.2.0-preview.1` app. It compiles and runs in the Editor (Mono) and in a Windows IL2CPP player with stripping Minimal. The results and the problems are in [`Probe/RESULTS.md`](Probe/RESULTS.md).

## Scope

| In scope | Out of scope |
|---|---|
| A new, minimal project from the published packages | The package `com.vsa.core` and its samples |
| Mono and IL2CPP with stripping Minimal | Stripping Low, Medium, High and `link.xml` |
| Reflection: scan, open and closed generics, settings | Fixes for the problems |
| A list of problems | The package rename |

## Repository state (2026-11-17)

| Item | State |
|---|---|
| Branch | `claude/umpire-unity-probe`, from `main` 60c4231 |
| `com.vsa.core` | VSA `0.1.0-preview.30`, not changed |
| Open PRs #20 and #21 | Test suite only, no change to the package |

## Get the probe

1. Set the token: `$env:GITHUB_TOKEN = gh auth token`.
2. In `Probe/`, run `dotnet run --file vendor.cs`. Git does not keep the DLLs.
3. Run `./probe.ps1 editor`.
4. Run `./probe.ps1 player -Stripping Minimal`.

To use another Umpire version, change `UmpireVersion` in `Probe/Vendor/UmpireVendor.csproj`, then do step 2 again.

## Files

| Path | Contains |
|---|---|
| `Probe/Assets/Probe/Posts/` | The basic app: requests, command, event, presenter, validator |
| `Probe/Assets/Probe/Lab/` | The reflection probes and their checks |
| `Probe/Assets/Probe/ProbeRunner.cs` | Composes the app and writes the `CHECK` lines |
| `Probe/Assets/Probe/Editor/ProbeEditor.cs` | Batch mode entries: run in the Editor, build the player |
| `Probe/probe.ps1` | Runs the Editor checks, or builds and runs the player |
| `Probe/vendor.cs` | Copies the Umpire DLLs to `Assets/Plugins/Umpire/` |

## Next stage

Use the same checks with stripping Low, Medium and High. Then test which stripping attributes and `link.xml` entries each check needs.
