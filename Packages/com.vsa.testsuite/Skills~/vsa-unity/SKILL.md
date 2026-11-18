---
name: vsa-unity
description: Read and wait for the state of a Unity Editor with the vsa-unity CLI (status, wait, watch, log). Use it before and after each step that drives an Editor from outside, when a command to the Editor times out, or when you must know if the Editor is ready, frozen, busy, in a dialog or crashed. It tells you what each exit code and each state means and what to do.
---

# vsa-unity

`vsa-unity` reads the state of one Unity Editor from outside and waits for a state with a time limit. Do not read `/api/status`, the descriptor or `unity status` to decide a state: they say `ready` during a freeze.

```bash
dotnet build Packages/com.vsa.testsuite/Client~/Cli
dotnet Packages/com.vsa.testsuite/Client~/Cli/bin/Debug/net8.0/vsa-unity.dll <command>
```

Run it from the Unity project folder, or pass `--project-path <path>`.

## When to use

- Before a step: `wait --until ready`.
- After a step that compiles, imports or enters Play Mode: `wait --until ready`.
- When a `unity command` call times out (exit 14): `status`, to see if the Editor is frozen or busy.
- To start an Editor and keep it healthy for a long run: `watch`.

## Commands

| Command | Changes the Editor | Does |
|---|---|---|
| `status` | No | The state now. `data.history` is false if no watch runs |
| `wait --until ready --timeout 600` | No | Waits for a state, or stops at once on a stop rule |
| `watch --timeout 3600` | **Yes** | Starts, answers dialogs, escalates, restarts one time |
| `log sessions` | No | The run folders, newest first |
| `log show [--run <id>] [--tail 50]` | No | The last lines of `unity.log` and `watch.ndjson` |
| `log mark "<text>"` | No | Adds a mark to `watch.ndjson` of the newest run |

- `--until` takes `ready` (`ready-idle`, `play-running`, `play-paused`) or a comma list of state names.
- `--pid <pid>` (status and wait only) reads an Editor that this tool did not start. `watch` refuses it.
- `watch` runs a batch-mode compile check before each start. With compile errors it does not start (`StartRefused`).
- Never stop an Editor that you did not start. Stop your own Editor only by its PID.

## Results

Each command writes one JSON envelope: `success`, `command`, `data`, `errors`, `warnings`. Each error has `code`, `message`, `hint` and `retryable`. `data.state` is the last state.

| Exit | Error codes | Do |
|---|---|---|
| 0 | none | Continue |
| 6 | `Stalled`, `Stuck`, `Loop` | Run `watch` to escalate, or stop your Editor by PID |
| 6 | `CompileFailed`, `SafeMode`, `StartFailed`, `StartRefused` | Fix the code. Do not start again before the fix |
| 6 | `Crash`, `ForeignEditor`, `NotAutomated`, `SupervisorFailed` | Read `log show`. Follow `hint` |
| 12 | `NoEditor` | Start the Editor with `watch`, or fail |
| 13 | `BadArguments`, `NotAProject`, `NotFound` | Fix the call |
| 14 | `Timeout` | Read `data.state`. Wait again, or escalate |
| 18 | `Dialog` | A native dialog blocks the Editor. Run `watch` to answer it |

## States

Waiting states have a cap and a stall window in `limits.json`. A cap of 0 means no cap. If no progress shows for the stall window, the result is `stalled`. If the state passes its cap, the result is `stuck`.

| Group | States | Meaning |
|---|---|---|
| No Editor | `no-editor`, `stale-descriptor`, `exited`, `foreign-editor` | No own Editor runs, or another tool owns the project |
| Failed | `start-failed`, `crash`, `safe-mode`, `compile-failed`, `not-automated` | The run cannot continue without a fix or a start |
| Dialog | `native-dialog`, `safe-mode-prompt`, `backup-prompt` | A native dialog waits for an answer |
| Waiting | `cold-start`, `settling`, `domain-reload`, `compiling`, `importing` | Unity works; wait |
| Waiting | `play-transition`, `long-command`, `queue-full`, `descriptor-mismatch` | The server or the queue is busy for a short time |
| Escalate | `server-down`, `frozen` | The server or the main thread does not answer |
| Ready | `ready-idle`, `play-running`, `play-paused` | The main thread answers and nothing waits |

- `frozen`: the server answers, the probe does not, and the beacon is still for 3 s. A long synchronous command also gives `long-command` or `frozen`.
- `long-command`: a command holds the queue, but the main thread runs. Wait.
- `status` with `history: false` takes one sample. Then `cold-start` can also be a domain reload, and no clock has run.

## Examples

A main thread that sleeps 90 s (live run of lab step A7). The probe gets HTTP 504 and the beacon is still:

```bash
vsa-unity wait --until ready --timeout 120
```

```json
{"success":false,"command":"wait","data":{"state":"frozen","result":"stalled","elapsedMs":64275,"pid":237852},
 "errors":[{"code":"Stalled","message":"frozen/stalled","hint":"Nothing moved for the stall window. Run 'vsa-unity watch' to escalate.","retryable":false}],"warnings":[]}
```

## Limits

- Windows only for the window signals (dialogs, busy window, hung flag). Other signals work on all platforms.
- `/api/dialog` needs Unity 6000.7 or later. On 6000.6 only the window signals see a dialog.
- License, Hub sign-in and Software Terms dialogs are not tested.
- The beacon runs only in an Editor started with `-vsaStatusBeacon` (`watch` adds it). Without the beacon, a freeze shows only through the probe.
