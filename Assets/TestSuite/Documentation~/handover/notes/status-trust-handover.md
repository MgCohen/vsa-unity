# Status trust: hand-over

Result: a tested status checker and supervisor for one Unity Editor, as a C# spike. 26 states, 12 signal sources, live runs of each forced case 3 times on Unity 6000.6.6f1, CLI 1.0.1, Pipeline 1.0.0, `-automated`, no focus.

- Page: https://claude.ai/artifact/BwivCXvSh9GhPwkNXSkG6H (decision table, per-case results, measured signals).
- Commit `0f24b8d` on branch `claude/status-trust-a9fc9a` (worktree `vcontainer-integration-samples-3fbf19`). No PR. `cli-tools.md` is not committed: its path and the tool name wait for the user.
- Result: 115 of 117 scored windows pass (`statuslab replay Tools/StatusLab/results/final`). The 2 fails are lost triggers from before a spike fix; the reruns pass 3 of 3.
- Field notes: `unity-field-notes` skill, sections "Status trust", "Start, crash and Safe Mode", "Server, ports, queue and game code".

## Where things are

| Path | Contains |
|---|---|
| `Tools/StatusLab/` | Spike: `Sources/` (12), `States/` (26 + `StateOrder.cs`), `Core/` (tracker, supervisor, sampler, limits), `Control/`, `Playbooks/` (A, B, C), `Replay/` |
| `Tools/StatusLab/results/final/` | The recordings that the page counts; `statuslab replay` checks them offline |
| `Tools/StatusLab/truth-overrides.json` | Truth corrections found after a run, each with a reason |
| `Assets/StatusLab/` | Editor scripts: force commands, beacon, hang, slow importer, server switch; `Runtime/PlayHang.cs` |
| `Packages/com.vsa.testsuite/Documentation~/cli-tools.md` | Draft VSA guideline for CLI tools (path and tool name need user OK) |

## Rules that matter most

1. Never trust `/api/status` or the descriptor heartbeat for a freeze. Use the beacon (still for 3 s).
2. Unity's busy window is a `#32770` window with "Skip Transcoding": not a dialog. Its title names the phase.
3. In `-automated` no start dialog shows: compile errors enter Safe Mode at once, a bad package exits with code 1. Run the batch-mode compile check before each start.
4. A crash keeps the process alive 24 to 31 s: read `Crash!!!` in the log.
5. CPU time is never progress. Caps catch silent but healthy work.
6. Save or discard dirty scenes by API before a test run.

## Next work items

- Move the spike into `Client~` (library, CLI `status` / `wait` / `watch`, skill). Keep the recordings as replay tests.
- Remove the code comments in `Assets/StatusLab/` that the VSA analyzer flags (VSA1013), or move the lab out of the analyzer scope.
- Gaps: license, Hub sign-in and Software Terms dialogs (not safe to force); `/api/dialog` needs 6000.7+.
- The peer worktree `statuslab-peer` (branch `claude/statuslab-peer`) is a throwaway for the foreign-descriptor case. Delete it when no longer needed.
