# Test suite lab

Fixtures for the end-to-end checks of `com.vsa.testsuite` (`Assets/TestSuite/Client~/Samples`).

| File | Content |
|---|---|
| `TestSuiteLab.unity` | An empty scene. The lab commands build the UI at run time |
| `LabRuntimeCommands.cs` | `labrt.*`: the lab button, the label and the click counter |
| `LabSuiteCommands.cs` | `labsuite.*`: state, cover, late show, drag targets, lazy start |
| `Editor/LabEditorCommands.cs` | `lab.freeze_now`: blocks the main thread, for the health probe check |
| `LabScripts.cs` | C# test scripts: `lab_click`, `lab_fail`, `lab_demo`, `lab_hang` |

The assembly works with and without the Input System. Without it, the event system uses `StandaloneInputModule` and the clicks use `ui-events`.
