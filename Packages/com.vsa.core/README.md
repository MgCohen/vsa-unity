# com.vsa.core

The VSA core for Unity 6000.6: the portable VSA DLLs (netstandard2.1), the VSA analyzers, the Unity host and the uGUI view bases.
A Unity app starts with one `VsaHost` component. Views inherit `PresenterComponent<T>` or `PresenterWatcher`.

## Contents

| Path | Contains |
|---|---|
| `Runtime/Plugins/` | The VSA DLLs and their dependencies |
| `Runtime/VSA.Analyzers.dll` | The VSA analyzers (Roslyn 4.10 build) |
| `Runtime/` | `VsaHost`, the Unity host, the view bases, `UnityLogProvider` |
| `Runtime/link.xml` | The IL2CPP keep rules of VSA |
| `Editor/` | `CompilerFiles` and `VsaLinker` |
| `Tests/` | EditMode and PlayMode tests |

## Start the app

Put `VsaHost` on one GameObject of the first scene.

```
VsaHost.Awake
├─ DontDestroyOnLoad          the app has the lifetime of the Unity app
├─ find the app class         App field, else the one app class, else DefaultVsaApp
└─ MyApp.Run(args)            VsaApp<TApp>
   └─ host scan               finds UnityVsaHost, the IVsaHost of this package
      └─ Run                  starts the IHost and returns at once
```

| `VsaHost` member | Use |
|---|---|
| `App` (field) | Full name of the app class. Empty: the scan |
| `Arguments` (field) | The `args` of `Run`, for example `--Modules:Counter:Max=5` |
| `Services` (static) | The services of the running app, else null |
| `IsRunning` (static) | True from `Run` until the app stops |
| `Stop()`, `Run()` | Stop the app, and start it again |

- An app class is a class that inherits `VsaApp<TApp>`, with no constructor parameters. If the project has none, `DefaultVsaApp` runs.
- The scan skips the test assemblies (they reference NUnit). A test selects its app class with the `App` field.
- A second `VsaHost` disables itself and does nothing. A scene change does not stop the app.

## The Unity host

| Item | Behavior |
|---|---|
| Builder | `Host.CreateEmptyApplicationBuilder`, plus command line, Unity log |
| Content root | `Application.dataPath` |
| Lifetime | `IHostLifetime` that does not wait for a signal |
| Presenter events | `LivePresenters.Dispatch` posts to the main thread |
| Stop | `Application.quitting`, play mode exit, domain reload, `VsaApp.Stop()` |

The empty builder is necessary: the default .NET host fails in Unity (EventLog logger on Mono, content root `Unity.exe\`).

## Views

```csharp
public sealed class CounterLabel : PresenterComponent<CounterPresenter> {
    [SerializeField] Text label;

    protected override void Render() => label.text = $"{Presenter.Value} of {Presenter.Max}";
}
```

| Base | Gets its presenters | Renders |
|---|---|---|
| `PresenterComponent<T>` | `Bind`, else an app scope in `Start` | On `Bind` and on `Changed` |
| `PresenterWatcher` | `Watch(a, b, ...)` | On a changed `Watch` and on `Changed` |

A factory calls `Bind`. A view in a scene with no `Bind` gets its presenter from a new scope of the running app.

- `Render` runs on the main thread. `Changed` from another thread goes to the main thread.
- `OnDestroy` stops the watch. A component that made its own scope disposes it. An override of `Start` or `OnDestroy` calls the base method.

## Analyzers

`Runtime/VSA.Analyzers.dll` has the label `RoslynAnalyzer` and no platform. Unity uses it as an analyzer, not as a library.

| Assembly | VSA rules | Rule values from |
|---|---|---|
| A consumer asmdef that references `VSA.Unity` | Yes | `Assets/Default.globalconfig` |
| `Assembly-CSharp`, `Assembly-CSharp-Editor` | Yes | `Assets/Default.globalconfig` |
| A consumer asmdef with no `VSA.Unity` reference | No | None |
| The package asmdefs | Off | `<AsmdefName>.globalconfig` |

- The analyzer folder sets the scope: Unity runs the analyzer on `VSA.Unity` and on each assembly that references it.
- Module asmdefs often do not reference `VSA.Unity`. The .NET test build checks the module code.
- A `<AsmdefName>.globalconfig` next to an asmdef replaces `Assets/Default.globalconfig` for that asmdef.
- The package code is framework code, so each package asmdef sets `VSA1000` to `VSA1014` to `none`.
## Editor scripts

| Script | Does |
|---|---|
| `CompilerFiles` | Writes `Assets/csc.rsp` and `Assets/Default.globalconfig` when the editor loads |
| `VsaLinker` | Gives `link.xml` and a rule for each app class to the UnityLinker |

- `Assets/csc.rsp` gets `-langversion:12`, the highest version of the Roslyn 4.10 compiler of Unity. It applies to each assembly with no own `csc.rsp`.
- `Assets/Default.globalconfig` gets the VSA rule values between the `# BEGIN com.vsa.core` and `# END com.vsa.core` lines. Unity reads only this name for all assemblies.
- Each script changes only its own lines. The menu item **Tools > VSA > Write Compiler Files** runs `CompilerFiles` again.
- The package code is C# 9, so it compiles before `Assets/csc.rsp` exists.

## IL2CPP

`Runtime/link.xml` keeps these types:

| Assembly | Kept | Reason |
|---|---|---|
| `VSA.Analyzers.Markers` | All | The marker attributes |
| `VSA.Handlers` | The four runners | `ActivatorUtilities` makes them |
| `VSA.Composition` | `VsaApp<TApp>` | `VsaHost` calls it by reflection |

`VsaLinker` adds each app class of the player assemblies, because `VsaHost` makes the app class by reflection.

## Open questions

- Runner rules: remove the `VSA.Handlers` rules when `IRunner` has `[RequireImplementors]` in VSA.
- First open: before `CompilerFiles` writes `Assets/Default.globalconfig`, VSA1000 stops the compile of each consumer assembly in the analyzer scope. Then `CompilerFiles` cannot run.
- App class rule: remove the `VsaLinker` app rule when `VsaApp<TApp>` has a linker marker in VSA.
