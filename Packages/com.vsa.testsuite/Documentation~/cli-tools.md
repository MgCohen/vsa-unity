# CLI tools

Each VSA tool is a C# library first. A thin CLI makes the library available to scripts, CI and agents. A skill tells agents when to call the CLI and what each result means. No agent decides inside a tool.

## Shape

```
C# caller ──────────────┐
                        ├─► Library    all logic, typed results
Script, CI, agent ─► CLI ┘   (thin: parse args, call library, write envelope, exit code)
Agent ─► Skill ─► CLI         (skill: when to call, how to read the result)
```

| Layer | Rule |
|---|---|
| Library | All logic. Typed results and typed errors. No console output. |
| CLI | Parses arguments, calls one library method, writes the envelope, returns the exit code. No logic. |
| Skill | Describes each command, each exit code and the action for each. It has no logic of its own. |

A test of the library is a test of the tool. The CLI needs only one test for each command: arguments in, envelope out.

## Commands

| Item | Rule | Example |
|---|---|---|
| Form | `<tool> <verb>` or `<tool> <group> <verb>` | `vsa-unity status`, `vsa-unity log show` |
| Verb | One word, present tense, lower case | `status`, `wait`, `watch`, `start`, `stop` |
| Option | Kebab case, long form | `--project-path`, `--timeout` |
| Target | Same options as the `unity` CLI | `--project-path`, `--pid`, `--port` |
| Read only | A verb that only reads never changes state | `status` reads; `watch` acts |

- Give each command one job. If a command reads and acts, split it.
- Mark each command that changes state in its help text and in the skill.

## Output

The default format is JSON on stdout. `--format human` gives text for people. Logs and progress go to stderr, never to stdout.

```json
{
  "success": false,
  "command": "wait",
  "data": { "state": "frozen", "since": "2026-11-17T15:30:12Z" },
  "errors": [ { "code": "Frozen", "message": "The main thread gave no answer for 60 s.",
                "hint": "Escalate, or run 'vsa-unity watch'.", "retryable": false } ],
  "warnings": []
}
```

- Use the envelope of the `unity` CLI: `success`, `command`, `data`, `errors`, `warnings`.
- Put the result of the library in `data`. Use the same names as the library types.
- Each error has `code` (PascalCase), `message`, `hint` and `retryable`. It can have `detail`.
- Write one JSON object. For a stream, use `--format ndjson`, one object on each line.

## Exit codes

Use the codes of the `unity` CLI, so one script can read both tools.

| Exit code | Meaning | Caller action |
|---|---|---|
| 0 | Success | Continue |
| 6 | Failed or refused: the request is valid, the result is not | Read `errors`, do not retry the same call |
| 12 | No target: no Editor or player found | Start the target, or fail |
| 13 | Not found or bad arguments | Fix the call |
| 14 | Time-out | Read `data` for the last state |
| 17 | Busy or not available now | Retry later |
| 18 | Waits for input, for example a dialog | Answer, or fail |

- Map each error `code` to one exit code. The README of the tool lists the map.
- Never change the exit code of an existing error code.

## Time limits and cancellation

- Each command that waits has `--timeout <seconds>` with a default. No command waits without a limit.
- On a time-out, exit 14 and put the last known state in `data`.
- Ctrl+C cancels the library call through a `CancellationToken`. The CLI then writes the envelope with `"code": "Cancelled"`.
- For work that takes more than a few minutes, start a job and return its id. Give a `status` command for the job.

## Configuration

Each value comes from the first source that has it:

1. A CLI option, for example `--log-root`.
2. An environment variable, `VSA_<TOOL>_<KEY>`, for example `VSA_UNITY_LOG_ROOT`.
3. The config file `<project>/.vsa/<tool>.json`.
4. The default in the library.

- Give each tool a `config show` command. It writes the values in use and the source of each.
- Time limits are config values, not constants in the code.

## Files and paths

- Write output only under one root folder, with a default inside the project, for example `<project>/Logs/<tool>/`.
- Give each run its own folder: `<root>/<runId>/`. `runId` is the UTC start time plus a short random value. Parallel and consecutive runs then never share a file.
- Make the folder name before the work starts. Write the process id inside the folder, not in its name.
- Keep the last N run folders. N is a config value.
- Print the path of each file that a person must read.

## Secrets

- Never print a token, a password or a key, also not in `--verbose` output or in an error.
- Read a secret into memory only. Do not write it to a log or a result file.

## Agents

- An agent calls the CLI and reads the envelope. It does not read raw signals or log files to decide a state.
- If the caller must act, put the action in the error `hint`, or in a `data.action` field.
- A command that changes state must be safe to run two times: the second call finds the state and does nothing.

## Skill

Each tool has one skill. The skill has these parts:

| Part | Content |
|---|---|
| When to use | The tasks that need the tool |
| Commands | Each command: purpose, options, reads or acts |
| Results | Each exit code and error code: meaning and action |
| Examples | One call and one envelope for each common task |
| Limits | What the tool cannot do, and what to do then |

Keep the skill short. The help text of the CLI is the full reference.

## Example: the Unity status checker

The status checker and the supervisor are one tool, `vsa-unity`, over the library in `Client~`.

| Command | Part | Changes the Editor | Result |
|---|---|---|---|
| `status` | Status checker | No | The state now, with its signals |
| `wait --until ready --timeout 600` | Status checker | No | Exit 0 when ready; exit 14, 18 or 6 with the state |
| `watch` | Supervisor | Yes | NDJSON stream of states and actions |
| `log sessions` | Library | No | Editor starts and runs, with paths |
| `log show --run <id>` | Library | No | Unity log lines and watch lines of one run |
| `log mark "<text>"` | Library | No | A mark in the watch log |

## Check list

- The library has all logic. The CLI has none.
- Each command has one job, and its help text says if it changes state.
- stdout has only the envelope. Logs go to stderr.
- Each error has `code`, `message`, `hint`, `retryable`, and one exit code.
- Each wait has `--timeout` and a default.
- Each run writes to its own folder.
- No secret in any output.
- The skill lists each command, each exit code and each action.
