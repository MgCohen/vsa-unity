# Pipeline lab scripts

These scripts are the test commands of the Unity CLI and pipeline experiments (Unity 6000.7.0b9, November 2026). The results are in the `unity-field-notes` skill of the `unity-worktrees` plugin.

To repeat a test on a different Unity version:

1. Copy `Editor/` and `Scripts/` to the `Assets/` folder of an empty project.
2. Add `com.unity.pipeline`, `com.unity.inputsystem` and `com.unity.ugui`.
3. For `LabScripts.cs` and `LabSuiteCommands.cs`, also add `com.vsa.testsuite`.
