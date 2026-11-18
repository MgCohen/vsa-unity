---
name: vsa-unity
description: Open, close and check a Unity Editor with the vsa-unity CLI (open, close, status, wait, log). Use it to open Unity and know when it is ready, before and after each step that drives an Editor from outside, when a command to the Editor times out, and to close Unity. It tells you what each exit code and each state means and what to do.
---

# vsa-unity

`vsa-unity` opens, closes and checks the Unity Editor of one project. A daemon of the project keeps the history of the Editor, answers dialogs and escalates hangs. Do not read `/api/status`, the descriptor or `unity status` to decide a state: they say `ready` during a freeze.

```bash
dotnet build Packages/com.vsa.testsuite/Client~/Cli
dotnet Packages/com.vsa.testsuite/Client~/Cli/bin/Debug/net8.0/vsa-unity.dll <command>
```

Run it from the Unity project folder, or pass `--project-path <path>`.

## When to use

- To open Unity: `open`. It returns when the Editor is ready.
- Before and after a step that compiles, imports or enters Play Mode: `wait --until ready`.
- When a `unity command` call times out (exit 14): `status`, to see if the Editor is frozen or busy.
- To close Unity: `close`.

## Commands

| Command | Changes the Editor | Does |
|---|---|---|
| `open [--timeout 600] [--observe]` | **Yes** | Starts or attaches, then waits for ready |
| `close [--save\|--discard]` | **Yes** | Quits; refuses with unsaved changes and no flag |
| `status` | No | The state now, from the daemon |
| `wait [--until ready] [--timeout 600]` | No | Waits for a state, or stops on a stop rule |
| `log sessions`, `log show`, `log mark "<text>"` | No | The run folders and their logs |

- `open` with no Editor: a batch-mode compile check, then a start with `-automated`. With compile errors it does not start (`StartRefused`).
- `open` with a running Editor (also one started by hand): the daemon attaches to it.
- Adopt is the default: the daemon answers dialogs, escalates a stall (quit, window close, stop by PID) and starts the Editor again one time. `--observe`: the daemon only answers dialogs.
- A second `open` uses the daemon that runs. The daemon ends when the Editor exits or after `close`.
- With no daemon, `status` and `wait` take their own samples: `data.history` is false.

## Results

Each command writes one JSON envelope: `success`, `command`, `data`, `errors`, `warnings`. Each error has `code`, `message`, `hint` and `retryable`. `data.state` is the last state.

| Exit | Error codes | Do |
|---|---|---|
| 0 | none | Continue. Read `warnings` |
| 6 | `Stalled`, `Stuck`, `Loop` | In adopt mode the daemon escalates. Wait again, or `close --discard` |
| 6 | `CompileFailed`, `SafeMode`, `StartFailed`, `StartRefused` | Fix the code. Do not open again before the fix |
| 6 | `UnsavedChanges`, `UnknownChanges` | Read `data.dirtyScenes`. `close --save` or `close --discard` |
| 6 | `Crash`, `SupervisorFailed`, `CloseFailed`, `DaemonFailed` | Read `log show` and `hint` |
| 12 | `NoEditor` | Run `open` |
| 13 | `BadArguments`, `NotAProject`, `NotFound` | Fix the call |
| 14 | `Timeout` | Read `data.state`. Wait again, or escalate |
| 17 | `Busy` | A close runs. Call again later |
| 18 | `Dialog` | No daemon answers the dialog. Run `open` |

| Warning | Meaning |
|---|---|
| `not-automated` | The attached Editor runs without `-automated`. Start dialogs can block it |
| `no-beacon` | The package is missing in the project. A freeze shows only through the probe |

## States

Waiting states have a cap and a stall window in `limits.json`. A cap of 0 means no cap. If no progress shows for the stall window, the result is `stalled`. If the state passes its cap, the result is `stuck`.

| Group | States | Meaning |
|---|---|---|
| No Editor | `no-editor`, `stale-descriptor`, `exited`, `foreign-editor` | No Editor runs, or no daemon tracks it |
| Failed | `start-failed`, `crash`, `safe-mode`, `compile-failed`, `not-automated` | The run cannot continue without a fix or a start |
| Dialog | `native-dialog`, `safe-mode-prompt`, `backup-prompt` | A native dialog waits for an answer |
| Waiting | `cold-start`, `settling`, `domain-reload`, `compiling`, `importing` | Unity works; wait |
| Busy | `play-transition`, `long-command`, `queue-full`, `descriptor-mismatch` | The server or the queue is busy for a short time |
| Escalate | `server-down`, `frozen` | The server or the main thread does not answer |
| Ready | `ready-idle`, `play-running`, `play-paused` | The main thread answers and nothing waits |

- `frozen`: the server answers, the probe does not, and the beacon is still for 3 s.
- `long-command`: a command holds the queue, but the main thread runs. Wait.
- While a state is `stalled` or `stuck`, each `wait` returns at once with that code. In adopt mode the daemon escalates in that time (quit, window close, stop) and starts the Editor again. Call `wait` again after some seconds.

## Examples

Open Unity:

```bash
vsa-unity open --timeout 300
```

```json
{"success":true,"command":"open","data":{"state":"ready-idle","phase":"running","mode":"adopt","attached":false,"pid":215440},"errors":[],"warnings":[]}
```

Close with an unsaved scene and no flag:

```json
{"success":false,"command":"close","data":{"state":"ready-idle","dirtyScenes":["Assets/Scenes/Main.unity"]},
 "errors":[{"code":"UnsavedChanges","message":"1 scene(s) have unsaved changes. Pass --save or --discard.","retryable":false}],"warnings":[]}
```

## Limits

- Windows only for the window signals (dialogs, busy window, hung flag). Other signals work on all platforms.
- `/api/dialog` needs Unity 6000.7 or later. On 6000.6 only the window signals see a dialog.
- License, Hub sign-in and Software Terms dialogs are not tested.
- One daemon for each project. A central daemon for all projects does not exist.
