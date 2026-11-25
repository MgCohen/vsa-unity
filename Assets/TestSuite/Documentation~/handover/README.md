# Test suite handover

The test suite and the status checker are on `main` (PR #20, merge commit `b9b53fe`, 2026-11-25). All work of the test suite session is on GitHub or on the pages below. The next work is the host design (K4 with X2).

## Where the work is

| Item | Place |
|---|---|
| Plan and state of each task | [Test Suite Stream Tracker](https://claude.ai/artifact/5W7EdkKmdTBedXtnTG5f3T) |
| Slice plan v6 | [Test Suite Slices](https://claude.ai/artifact/QieaRfy7ST74BqneCmdZGY) |
| Stage T plan (test model) | [Stage T plan](https://claude.ai/artifact/TRi8xrsX74x9FSsyspobCD) |
| Stage F plan (trigger cases) | [Stage F plan](https://claude.ai/artifact/Q8XDN648hMQQ1Byb1z6VFm) |
| Stage U plan | [Stage U plan](https://claude.ai/artifact/RTqUDi2zyW88SopNg3duhB) |
| Engine findings | [Spike Engine Findings](https://claude.ai/artifact/218Ahu1iRzvbrgyRxoE3R7) |
| Status trust results | [Status trust page](https://claude.ai/artifact/BwivCXvSh9GhPwkNXSkG6H) |
| Code that the cleanup deleted | Tag `testsuite-before-cleanup` |
| Untracked files of the spike worktree | Branch `wip/upm-unity-esa-ded344` |
| Original notes | `notes/` in this folder |

## Decisions

| Decision | Reason |
|---|---|
| The test suite is in `Assets/TestSuite` | It moves to `Packages/` when it is stable and complete |
| The dice project is the unifying sample | One sample shows all tools |
| The test suite and the status checker do not call each other in code | Each tool must be stable alone first |
| No helper, recorder or logger before a real need | Each addition starts as one minimal thing |
| The package must work with only the Unity Editor | Consumers install nothing more |
| No change goes to `main` without the approval of the user | Rule of the user |

## Open items

| Item | What is missing |
|---|---|
| K4 and X2 | The host design: an Editor host, a Commands root beside App, and sessions across Play Mode. Only one Umpire app can run until then, and the status checker examines Editors only |
| G4 and G7 | 14 engine errors `UMP020101`. G4 waits for vsa-template#358. G7 waits for K4 |
| T0, T1 | The API samples of the user and the answers to T1 to T9 |
| F0 | The answers to FQ1 to FQ9. V3, V4 and V5 use them |
| T5, T6 | How test scripts are found, created and started |
| X1 | Product names and the path of the CLI guideline |
| Release | The package version is `0.1.0-preview.27`. The tag of that version does not have the fix for #49 |
| `UMP030001` | The engine stops when `VSA.TestSuite.Editor.dll` is in the bin folder. The cause is not known, and no upstream issue exists |
| Gate cleanup | Android icon noise in `ProjectSettings.asset` |

## Run the checks

### Unity and client checks

1. Run `./run-tests.ps1`. The last results are EditMode 31/31 and PlayMode 59/59.
2. Run `./il2cpp-check.ps1`. The log must show `argument=42` and `argument-order=process`.
3. Run `dotnet test Assets/StatusChecker/Client~/Tests`. The last result is 65/65.

### Lab scripts

`<P>` is the path of the project.

1. Run `unity open <P> --args "-automated" --until-ready`.
2. Open the lab scene:
   ```bash
   unity command --project-path <P> script eval --code 'return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TestSuiteLab/TestSuiteLab.unity").name;'
   ```
3. Run `unity command --project-path <P> editor.playmode start`.
4. Run `session.info` until it answers.
5. Before each script, run `scenes.load --name TestSuiteLab`.
6. Run `test_suite.run --name <script>`.
7. Run `session.end`, then `editor.playmode stop`, then `editor quit --discard`.

The scripts are `lab_touch`, `lab_objects`, `lab_wait`, `lab_button`, `lab_missing`, `lab_scene`, `lab_scene_end`, `lab_scene_parallel` and `lab_reset`. `lab_missing` fails by design.

### Umpire engine

The engine is `engine/net8.0` from the vsa-template release `v0.2.0-preview.24`.

1. Compile the project in Unity.
2. Copy `Library/ScriptAssemblies` to a new folder `<bin>`.
3. Delete `VSA.TestSuite.Editor.dll` from `<bin>`.
4. Write `Assets/TestSuite/.umpireconfig`. Do not commit it:
   ```ini
   [umpire.check]
   references = "<bin>, <P>/Packages/com.umpire.unity/Runtime/Plugins, <Unity Editor>/Editor/Data/Managed/UnityEngine"
   source_root = "<P>"
   project_checks = false
   ```
5. Run the engine:
   ```bash
   dotnet umpire-engine.dll --project <P> --block Assets/TestSuite --bin <bin> --stage Check
   ```
6. Make sure that it gives 14 errors.
7. Delete the `artifacts~` folders that it wrote.

The dice sample gives 0 errors with `--block Assets/DiceGame`. It also gave 0 errors with the package from the git URL at `47e03d7`.

## Do not commit

- `Assets/TestSuite/.umpireconfig`
- `Assets/Settings/` and `Assets/Settings.meta`
- `ProjectSettings/PackageManagerSettings.asset`
