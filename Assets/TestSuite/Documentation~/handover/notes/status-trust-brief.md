# Brief: Editor status trust spike and playbook

Start this in a new session of the vsa-unity project, in its own worktree, cut from `claude/testsuite-spike`.

## Background

The vsa-unity repo holds `com.vsa.testsuite`, a test suite package that drives a Unity Editor or player from outside with the Unity CLI and `com.unity.pipeline` 1.0.0 (`[CliCommand]`). The feature F1 "instance control and health" needs a watchdog: a supervisor outside Unity that must know the true Editor state before and after each step.

Earlier tests showed that some signals lie:

- `unity status` shows `starting` on a dialog.
- `/api/status` and its heartbeat stay `ready` during a frozen main thread, because the server runs on another thread.
- One Editor runs one command at a time, so a probe time-out can be a busy queue, not a freeze.

The spike work on the test suite runtime (branch `claude/testsuite-spike`, PR MgCohen/vsa-unity#21) is on hold while the user finishes the VSA template package. This status work runs in parallel with a planning session. The project uses Unity 6000.6.6f1 on Windows 11.

## Task

Find which Editor states exist, and how an outside client can detect each one correctly.

1. Load the `unity-field-notes` skill first. It has our tested results (dialogs, Safe Mode, freeze, command queue, timeouts). Also read the docs review notes in `C:\Users\MATHEU~1.COH\AppData\Local\Temp\claude\90a32a96c68924d6\3cd7ba6c-29fb-4019-a5b2-1b42fe37ebb7\scratchpad\docs-review\` if they still exist.
2. Make a state table. Minimum states: cold start and import, settling, ready idle, compiling, compile failed, domain reload, Play Mode enter and exit, Play Mode running, a modal dialog (native and `DisplayDialog`), startup dialogs (Safe Mode, scene backup), a long command that holds the queue, a frozen main thread, a crash or a stale descriptor, no Editor.
3. For each state, write what each signal shows: `GET /api/status`, `/api/progress`, `/api/dialog` (6000.7+ only), `unity status`, `unity pipeline editors --format json`, a cheap main-thread probe with a short timeout, `console status` `groundTruth`, the descriptor file, the process (PID alive, CPU).
4. Plan one or two small spike playbooks that force each state on purpose and read all signals. Run each state several times, so we know if the result is stable. Then write the rule: which signals together give the correct state, and which signal lies.
5. Write the result as a decision table: state → detection rule → action (wait, retry, answer dialog, restart, fail).

## Rules

- Work only in your own worktree. Start your own Editor with `unity open . --args "-automated" --until-ready --timeout 600`. Do not touch an Editor you did not start. Stop Editors and players only by PID, never by process name.
- Never print the Pipeline bearer token (`evalToken`). Do not give any window the focus without asking. Do not change security settings.
- Do not upgrade the project from Unity 6000.6.6f1. If a test needs 6000.7 (the dialog API), write it down as a gap.
- Do not change the spike code in `Packages/com.vsa.testsuite/Spike/` or the old package code. Put the probe commands and playbooks in a new folder, for example `Assets/StatusLab/`.
- Write all prose in STE. One type in each file. Minimal design, no extra validation.
- Do not commit `Assets/Settings/`, `Assets/Settings.meta` or `ProjectSettings/PackageManagerSettings.asset`.

## Done means

- The state table with the signal values.
- The detection rules and the decision table.
- The playbooks.
- New tested facts added to the `unity-field-notes` skill.
- A commit on its own branch. No PR.
- A report with the commit and the 3 most important findings.
