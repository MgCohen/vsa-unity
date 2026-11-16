# com.vsa.testsuite

Drives a running game from the `unity` CLI and from C# scripts. Commands call actions, actions call systems, and hooks record evidence for each step.
The package needs `com.unity.pipeline` 1.0.0 and Unity 6000.6 or later. It has no VSA reference.

## Install

Add the package to `Packages/manifest.json`:

```json
"com.vsa.testsuite": "https://github.com/MgCohen/vsa-unity.git?path=/Packages/com.vsa.testsuite#latest"
```

The Pipeline server must run: in the Editor it runs by default. In a player, it runs only in a development build with the runtime pipeline on.

## Layers

```
unity command ui.click_object --path Canvas/Play      1 command: CLI args to one action
  └─ Actions.Run                                         static entry, builds the runtime on the first call
     └─ ActionPipeline                                   hooks before and after each action
        └─ ClickObjectHandler                            2 action: routing only
           ├─ ctx.Run(FindObject)  → ObjectSystem.Find   3 system: all logic
           └─ ctx.Run(ClickScreen) → PointerSystem.Click
                                       └─ IPointerInput  4 input path
```

| Layer | Rule |
|---|---|
| Command | Maps the CLI args to one action. No logic. |
| Action | Calls one system, or other actions with `ctx.Run`. No logic. |
| System | All logic. A plain class, no base class. |
| Hook | Work for every action: log, console, step record, screenshot. |

## Commands

| Command | Action | Result |
|---|---|---|
| `ui.find_object --path P` | `FindObject` | Path and screen point |
| `ui.click_screen --x X --y Y` | `ClickScreen` | Point, top UI object, input path, frames |
| `ui.click_object --path P` | `ClickObject` (composite) | Same as `click_screen` |
| `ui.wait_for_object --path P [--state Active] [--timeout-ms 10000]` | `WaitForObject` | Time and frames |
| `ui.assert_state --path P [--state Active]` | `AssertState` | The state |
| `ui.assert_text --path P --text T` | `AssertText` | The text |
| `game.wait_frames [--count 1]` | `WaitFrames` | Time and frames |
| `game.wait_seconds [--seconds 1]` | `WaitSeconds` | Time and frames |

A state is `Active`, `Inactive` or `Missing`. A path starts at a root object of a loaded scene, for example `Canvas/Menu/Play`.

Each command writes one JSON result:

```json
{ "ok": true, "action": "ui.click_object", "step": "s-003_ui-click_object", "ms": 32.4,
  "result": { "Point": [489, 249], "Hit": "LabButton", "Adapter": "input-system-queue" } }
```

## Failures

| Code | Exit code | HTTP | Cause |
|---|---|---|---|
| `NotFound` | 13 | 400 | No object at the path. `detail` lists the names that exist. |
| `NotActive` | 6 | 400 | The object is not active. |
| `Blocked` | 6 | 400 | Another UI object covers the target. No click is sent. |
| `AssertFailed` | 6 | 400 | `detail` has `expected` and `actual`. |
| `TimedOut` | 14 | 504 | A wait reached its time limit. |
| `NoDevice` | 6 | 400 | No mouse or no EventSystem. |

A failed command also writes the result, with `code`, `reason`, `detail` and the step `folder`. Over HTTP, read the body of the 400 or 504 response.

## Evidence

Each runtime writes to one run folder. In the Editor the folder is `Runs/` beside `Assets/`. In a player it is `Application.persistentDataPath/Runs/`.

```
Runs/2026-11-16_053816/
  s-003_ui-click_object/step.json     the action tree with arguments, times and results
  s-009_ui-click_object/fail.png      screenshot, only in Play Mode, only on failure
  s-009_ui-click_object/console.json  log lines of the step, on failure or on a warning
```

Add `Runs/` to `.gitignore`.

## Input paths

| Input path | When | Reaches |
|---|---|---|
| `input-system-queue` | `com.unity.inputsystem` is installed | All game code that reads the mouse |
| `ui-events` | No Input System | uGUI objects only |

The Input System path queues state events and does not call `InputSystem.Update()`. In Editor Play Mode, a manual update loses the event. A click takes 3 frames: move, press, release.

## Scripts inside Unity

A C# script calls the runtime directly, not the CLI. The hooks and the step folders are the same.

```csharp
var run = ActionRuntime.GetOrBuild();
await run.RunStep(new ClickObject("Canvas/Menu/Play"), cancel);
await run.RunStep(new WaitForObject("Canvas/Hud"), cancel);
await run.RunStep(new AssertText("Canvas/Hud/Score", "0"), cancel);
```

## Add your own command

1. Write the system. It holds the logic.
2. Write the action as a record, and a handler that calls the system.
3. Write the command. It maps the CLI args to the action.
4. Add the handler to the composition root.

```csharp
public sealed class WalletSystem { public long Add(long amount) { /* game code */ } }

[ActionName("wallet.add")]
public sealed record WalletAdd(long Amount) : IAction<long>;

public sealed class WalletAddHandler : ActionHandler<WalletAdd, long>
{
    readonly WalletSystem wallet;
    public WalletAddHandler(WalletSystem wallet) => this.wallet = wallet;
    protected override Task<long> Handle(WalletAdd a, ActionContext ctx) => Task.FromResult(wallet.Add(a.Amount));
}

static class WalletCommands
{
    [CliGroupCommand("wallet", "Add currency")]
    internal static Task<ExitCode> Add(CommandContext cmd, [CliArg("amount", "Amount")] long amount)
        => Actions.Run(cmd, new WalletAdd(amount));
}
```

Set your composition root before the first command, for example in a `[RuntimeInitializeOnLoadMethod]`:

```csharp
ActionRuntime.Factory = () =>
{
    var root = new TestRoot();
    var handlers = root.DefaultHandlers().Add(new WalletAddHandler(new WalletSystem()));
    var hooks = root.DefaultHooks();
    return root.Build(handlers, hooks);
};
```

To replace a default part, give your own part to the root, for example `new TestRoot(new MyPointerInput())`, or add your own handler for the same action. The last handler added for an action type is used.

## Limits of version 0.1

- A domain reload stops a running action. The next command builds the runtime again.
- Only Editor Play Mode is tested. Players are not tested yet.
- No reset, scene or capture actions yet.
