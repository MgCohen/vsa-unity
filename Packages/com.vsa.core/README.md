# com.vsa.core

The VSA core for Unity 6000.6: the portable VSA DLLs (netstandard2.1), the VSA analyzers, the Unity host and the uGUI view bases.
A Unity app starts with one `VsaHost<TApp>` component. Views inherit `PresenterComponent<T>`.

## Install

1. Copy [`Documentation~/Default.globalconfig`](Documentation~/Default.globalconfig) to `Assets/Default.globalconfig` before you add the package.
2. Add the package to `Packages/manifest.json`:

```json
"com.vsa.core": "https://github.com/MgCohen/vsa-unity.git?path=/Packages/com.vsa.core#latest"
```

The tag `latest` is the last commit of `main`. To get a later commit, click **Update** in the Package Manager.

Step 1 is necessary. Without the file, error VSA1000 stops the compile of each consumer assembly in the analyzer scope, so the editor script cannot write the file.

## Contents

| Path | Contains |
|---|---|
| `Runtime/Plugins/` | The VSA DLLs and their dependencies |
| `Runtime/VSA.Analyzers.dll` | The VSA analyzers (Roslyn 4.10 build) |
| `Runtime/` | `VsaHost<TApp>`, the Unity host, `PresenterComponent<T>`, `UnityLogProvider` |
| `Runtime/link.xml` | The IL2CPP keep rule of VSA |
| `Documentation~/Default.globalconfig` | The VSA rule values for the install step |
| `Editor/` | `CompilerFiles` and `VsaLinker` |
| `Tests/` | EditMode and PlayMode tests |

## Start the app

Write the app class and one host component, each in its own file. The file name of the host is its class name.

```csharp
public sealed class MyApp : VsaApp<MyApp> {
    protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseUnityHost();
}

public sealed class GameHost : VsaHost<MyApp> { }
```

Put `GameHost` on one GameObject of the first scene.

```
GameHost.Awake
├─ DontDestroyOnLoad          the app has the lifetime of the Unity app
└─ MyApp.Run(args)            VsaApp<TApp>, a direct call
   ├─ UseUnityHost            selects UnityVsaHost, the IVsaHost of this package
   └─ Run                     starts the IHost and returns at once
```

| `VsaHost` member | Use |
|---|---|
| `Arguments` (field) | The `args` of `Run`, for example `--Modules:Counter:Max=5` |
| `AppType` | The app class, `TApp` |
| `Services` (static) | The services of the running app, else null |
| `IsRunning` (static) | True from `Run` until the app stops |
| `Owner` (static) | The host component that owns the app, else null |
| `Stop()`, `Run()` | Stop the app, and start it again |

- If the app does not call `UseUnityHost`, the host scan of VSA finds `UnityVsaHost`.
- A second `VsaHost` disables itself and does nothing. A scene change does not stop the app.

## The Unity host

| Item | Behavior |
|---|---|
| Builder | `Host.CreateEmptyApplicationBuilder`, plus command line, Unity log |
| Content root | `Application.dataPath` |
| Lifetime | `IHostLifetime` that does not wait for a signal |
| Presenter events | `LivePresenters.Dispatch` runs them after `Awaitable.MainThreadAsync()` |
| Stop | `Application.quitting`, play mode exit, domain reload, `VsaApp.Stop()` |

The empty builder is necessary: the default .NET host fails in Unity (EventLog logger on Mono, content root `Unity.exe\`).

## Views

```csharp
public sealed class CounterLabel : PresenterComponent<CounterPresenter> {
    [SerializeField] Text label;

    protected override void Render() => label.text = $"{Presenter.Value} of {Presenter.Max}";
}
```

| Item | Behavior |
|---|---|
| Presenter | `Bind`, else an app scope in `Start` |
| Render | On `Bind` and on `Changed`, on the main thread |
| `Bind` again | Same presenter: nothing. Other presenter: stops to listen to the last one. |
| `OnDestroy` | Stops to listen. Disposes the scope that the view made. |

- A factory calls `Bind`. A view in a scene with no `Bind` gets its presenter from a new scope of the running app.
- An override of `Start` or `OnDestroy` calls the base method.

### Scene views

Unity makes the views of a scene, so no factory calls `Bind`. The `Start` fallback gives these views a presenter. It has these limits:

| Limit | Result |
|---|---|
| Each scene view makes its own scope | Two views of one presenter type get two presenters. They do not share state. |
| The view reads the static `VsaHost.Services` | A service locator: an exception to the composition rule of VSA |
| No app runs at `Start` | The view gets no presenter and shows no error |

If a consumer needs shared presenters or injection into scene objects, use a DI library for Unity, for example VContainer. Call `Bind` from its scene scope. A view that has a presenter before `Start` does not use the fallback.

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
| `CompilerFiles` | Updates `Assets/Default.globalconfig` |
| `VsaLinker` | Gives `Runtime/link.xml` to the UnityLinker |

- `Assets/Default.globalconfig` gets the VSA rule values of `Documentation~/Default.globalconfig` between the `# BEGIN com.vsa.core` and `# END com.vsa.core` lines. Unity reads only this name for all assemblies. After a package update, the script writes the rule values of the new VSA version.
- The script changes only its own lines. The menu item **Tools > VSA > Write Compiler Files** runs `CompilerFiles` again.
- The package code is C# 9, the default language version of Unity 6000.6.

## IL2CPP

VSA marks the bases that it makes from a `Type` with `[RequireImplementors]` or `[RequireDerived]`, so the linker keeps them. `Runtime/link.xml` keeps one more assembly:

```xml
<assembly fullname="VSA.Analyzers.Markers" preserve="all" />
```

The scan reads `[AutoRegister]` on the VSA bases. Without the rule, stripping High removes the markers assembly. The UnityLinker reads the `link.xml` files of `Assets/` only, so `VsaLinker` gives the package file.

## Open questions

- Automatic first install: the install step 1 is manual. A GitHub issue tracks a fix in VSA.Analyzers or an opt-in analyzer asmdef.
- Stripping: does the package need `link.xml` and `VsaLinker`, or does a consumer use stripping Minimal only?
