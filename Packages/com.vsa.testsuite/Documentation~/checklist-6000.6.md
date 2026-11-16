# Test suite checklist: Unity 6000.6 in vsa-unity

This checklist examines `com.vsa.testsuite` (PR #20) in the vsa-unity project on Unity 6000.6.6f1. The earlier tests used `pipeline-lab` on Unity 6000.7. A review subagent checked the claims against the code before the run. Its corrections are in this version.

Run on 2026-11-16. Result: all items are `working`, except the items in [Open items](#open-items).

## Differences from the earlier tests

| Item | pipeline-lab (earlier) | vsa-unity (this run) |
|---|---|---|
| Unity | 6000.7.0b9 | 6000.6.6f1 |
| Input System | 6.7.0 | Not installed. Clicks use `ui-events`. Legacy input (`activeInputHandler: 0`) |
| Unity Recorder | 5.1.8 | Not installed. `record.*` gives `NoDevice` |
| Render pipeline | URP | Built-in |
| Lab fixtures | `Assets/Scripts`, `SampleScene` | `Assets/TestSuiteLab`, `TestSuiteLab` scene |

## Result values

`working`: passes. `failed`: does not pass. `blocked`: cannot run now. `n/a`: does not apply. `skipped`: the check ran but did not examine its claim.

## A. Setup

| ID | Claim | Check | Expected | Result |
|---|---|---|---|---|
| A1 | The project compiles with the package, with no Input System and no Recorder | `unity recompile` in a batch mode Editor | 0 errors. No `VSA.TestSuite.Recorder` assembly | working |
| A2 | The lab fixtures compile without the Input System | `Assets/TestSuiteLab` with its asmdefs | `VSA.TestSuite.Lab` and `VSA.TestSuite.Lab.Editor` load | working |
| A3 | The lab scene is in the build list | `EditorBuildSettings.scenes` | `Assets/TestSuiteLab/TestSuiteLab.unity` | working |
| A4 | The Standalone build has the runtime Pipeline | `ENABLE_RUNTIME_PIPELINE_DEV` in the Standalone defines | Present | working |
| A5 | The player loop runs without focus | `PlayerSettings.runInBackground` | True | working |
| A6 | Run folders are not in git | `.gitignore` | `/Runs/` | working |

## B. Unity tests (`./run-tests.ps1`, with the Editor closed)

| ID | Claim | Expected | Result |
|---|---|---|---|
| B1 | The package EditMode tests pass | `VSA.TestSuite.Editor.Tests`: 15 of 15 | working |
| B2 | A failed `Before` hook does not stop `After` for earlier hooks | `FailedBeforeHookStillGivesAfterToEarlierHooks` | working |
| B3 | `RecordingSystem` stops safely | 7 tests in `RecordingSystemTests` (no recorder needed) | working |
| B4 | The other vsa-unity tests still pass | EditMode 27 of 27, PlayMode 30 of 30 | working |

## C. Editor Play Mode (`dotnet run --file e2e.cs -- <vsa-unity path> TestSuiteLab`)

Result: 25 of 25 pass.

| ID | Check | Result | Note |
|---|---|---|---|
| C1–C14 | health, Play Mode, click, assert, Blocked, NotFound, waits, unknown command, 10 clicks, long action, hold, drag | working | All clicks, the hold and the drag use `ui-events` |
| C15 | reset of an unsaved scene fails clearly | skipped | The Editor opens the saved lab scene. The check runs only from an Untitled scene |
| C16–C17 | reset loads the scene, seed repeats values | working | |
| C18 | load scene by name | working | In the Editor, the load also finds scenes outside the build list. F covers the build list path |
| C19–C24 | unknown scene, scripts, exit Play Mode | working | |
| C25 | frozen main thread is Frozen | working | Needs `lab.freeze_now` (`Assets/TestSuiteLab/Editor`). The check now fails clearly if the command is missing |

## D. Evidence

| ID | Claim | Expected | Result |
|---|---|---|---|
| D1 | Each root step writes `step.json` | 39 of 39 | working |
| D2 | `summary.json` has one row for each root step | 39 rows, 32 passed, 7 failed | working |
| D3 | A failed step writes `console.json` | Present for each failed step | working |
| D4 | A failed step writes `fail.png` | A PNG file | failed: see [Open items](#open-items) |
| D5 | The success result has `folder` | The step folder | working |

## E. Recording and overlay without the Recorder

| ID | Claim | Expected | Result |
|---|---|---|---|
| E1 | `record.status` gives the reason | `Recorder: none`, the text names `com.unity.recorder` | working |
| E2 | `record.start` fails clearly | `NoDevice`, exit code 6 | working |
| E3 | `record.stop` with no recording does nothing | `ok: true`, result null | working |
| E4 | `suite.run` runs with no video | Passes, no `.mp4` | working |
| E5 | The overlay takes no raycasts | No raycaster on `VSA.TouchOverlay`. `Blocked` comes only from the cover | working |
| E6 | `ui.show_touches --on false` hides it | No overlay image becomes active after a click | working |
| E7 | `ui.find_object` finds objects in `DontDestroyOnLoad` | `ui.find_object --path VSA.TouchOverlay` succeeds | working (fixed in this run) |
| E8 | `record.cs` | Needs the Recorder | n/a |

## F. Windows development player (Mono), built with 6000.6

| ID | Claim | Check | Result |
|---|---|---|---|
| F1 | The development build has the Pipeline server | Build log: "Pipeline: Runtime server ENABLED in build." | working |
| F2 | `player.cs` passes | `dotnet run --file player.cs -- Build/TestSuiteLab/Lab.exe "-batchmode"`: 10 of 10 | working |
| F3 | The lab scripts are found in the player | `lab_click` passes | working. Stripping is off for Mono here, so this does not test stripping |
| F4 | Recording in a player gives `NoDevice` | New check in `player.cs` | working |

The player ran with `-batchmode`, so it opened no window and took no focus.

## G. Clean-up

| ID | Claim | Result |
|---|---|---|
| G1 | Only intended changes are in git: fixtures, settings, package fixes, tests | working |
| G2 | Only the Editors and players that the test started were stopped, by PID | working |

## Open items

| ID | Problem | Cause | Action |
|---|---|---|---|
| D4 | No `fail.png` in a 6000.6 batch mode Editor | `ScreenCapture.CaptureScreenshot` writes no file in 6000.6 batch mode, also after 30 frames. 6000.7 batch mode writes it | The hook now logs a warning into `console.json`. Not tested: a 6000.6 Editor with a window |
| C15 | The unsaved scene check did not run | The Editor opens a saved scene | Run it from an Untitled scene |
| — | Stripping is not tested | Mono with stripping off | Run `player.cs` on an IL2CPP build |
| — | `ui-events` limits | No pointer enter or exit, no drag threshold, no `initializePotentialDrag` | Document. Use the Input System path for full UI behavior |
| — | `IsExternalInit` is public in `VSA.TestSuite` | A second public copy in another assembly would be ambiguous | Low risk now. The other copies in vsa-unity are internal |
