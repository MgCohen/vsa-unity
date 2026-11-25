# VSA Test Suite

Drives a running game from the `unity` CLI and from C# scripts, with commands and evidence for each step.
It needs `com.unity.pipeline` 1.0.0, `com.umpire.unity` and Unity 6000.6 or later. Videos need `com.unity.recorder` 5.0 or later.

The test suite is a folder of this project (`Assets/TestSuite`) for now, not a package. Its package dependencies are in `Packages/manifest.json`.

| Folder | Contains |
|---|---|
| `App/` | The Umpire app: commands, slices and the session |
| `Editor/` | The video recorder of the Editor |
| `Assets/TestSuiteLab/` | The lab scenes and the lab scripts |

## Commands

The Pipeline server must run. In the Editor it runs by default. In a player, it runs only in a development build with the runtime pipeline on.

| Group | Commands |
|---|---|
| `touch` | `screen`, `object`, `hold_screen`, `hold_object`, `drag_screen`, `drag_object`, `show` |
| `objects` | `find`, `wait_for`, `assert_state`, `assert_text` |
| `scenes` | `load`, `reset` |
| `wait` | `frames`, `seconds` |
| `evidence` | `screenshot`, `folder` |
| `recording` | `start`, `stop`, `status` |
| `test_suite` | `list`, `run` |
| `session` | `info`, `end` |

Each session writes to one run folder. In the Editor the folder is in `TestRuns/` beside `Assets/`. In a player it is in `Application.persistentDataPath/TestRuns/`.

The input goes to uGUI objects only, through the EventSystem.

Only one Umpire app can run. If another app runs in the scene, the test suite app does not start.
