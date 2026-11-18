# Umpire 0.2.0-preview.1 in Unity: probe results

A minimal Umpire app compiles and runs in Unity 6000.6.6f1, in the Editor (Mono) and in a Windows IL2CPP player (stripping Minimal). Umpire's own reflection works on IL2CPP: the module scan, open classes closed over structs, open handlers and validators that no code names. Four problems occurred, and one observation needs the stripping tests. This file lists the problems only. It gives no fixes.

## Setup

| Item | Value |
|---|---|
| Unity | 6000.6.6f1, Windows 64-bit |
| Umpire packages | `0.2.0-preview.1` (GitHub Packages, 2026-11-17) |
| Project | `Probe/`, a new empty Unity project |
| Package source | 48 DLLs in `Assets/Plugins/Umpire/`, from `vendor.cs` |
| App code | `Assets/Probe/`, no asmdef (`Assembly-CSharp`) |
| Analyzers | Not installed |
| IL2CPP settings | Stripping Minimal, code generation `OptimizeSpeed` |

The app has two modules. `Posts` is the basic app: add a post, read posts, rename a post, an event, a presenter. `Lab` holds the reflection probes. The runner composes the app with `new Vsa<VsaHost>().CreateHost().Compose(services, app)`, with no host run.

## How the tests run

```powershell
./probe.ps1 editor                         # Unity batch mode, -executeMethod, Mono
./probe.ps1 player -Backend IL2CPP         # player build (IL2CPP or Mono2x), then the player runs the checks
```

Each check writes one line: `CHECK <name> ok <detail>` or `CHECK <name> FAIL <exception chain> | <stack>`. A failed check does not stop the other checks. The logs are in `Results/`.

## Tests and results

| Check | Why | Mono | IL2CPP |
|---|---|---|---|
| `compose` | `Compose` runs all scans and registrations | ok | ok |
| `modules` | The scan finds module classes (`Activator.CreateInstance`) | ok | ok |
| `request`, `command`, `event` | The pipeline finds handlers that no code names | ok | ok |
| `presenter` | A presenter sends a request | ok | ok |
| `validator` | A `Validator<T>` that no code names runs | ok | ok |
| `open-class:Article` | Umpire closes `Store<T>` over a class | ok | ok |
| `open-class:Score`, `:Int32` | Umpire closes `Store<T>` over a struct | ok | ok |
| `open-class:Mentioned` | Control: code names `Store<Mentioned>` | ok | ok |
| `open-handler:*` | An open event handler, closed over a class and structs | ok | ok |
| `di-open-generic:Article` | Microsoft DI closes an open registration, class | ok | ok |
| `di-open-generic:Score`, `:Int32` | Microsoft DI closes an open registration, struct | ok | **FAIL** (P2) |
| `settings` | `Module<TSettings>` binds settings by reflection | ok | ok |
| `logger` | `ILogger<T>` resolves | ok | ok |
| `generic-host` | `App.Run` with the .NET Generic Host | **FAIL** (P3) | **FAIL** (P3, P4) |

Details of the checks:

- `open-class` reads the closed type from `AppTypes.Registered()`, so no code names `Store<Score>`. `Store<T>` uses `List<T>`, `HashSet<T>` and `EqualityComparer<T>` on the closed type.
- `open-handler` publishes `Changed<T>`. No code names `ChangedHandler<Score>` or `ChangedHandler<int>`.
- The type arguments come from `[TypeArgument]` on a class, on a struct, and from `[assembly: TypeArgument(typeof(int))]`.
- `settings` binds a string, an int, a `List<string>`, an enum and a `Dictionary<string, int>`. No code makes the dictionary.

## Unity host and shell

`Assets/Umpire.Unity/` (asmdef `Umpire.Unity`) holds the Unity part: `UnityVsaHost`, `Shell<TPack>`, `VsaShell` and `UnityLogProvider`. The shell makes the app itself, so an app needs no app class. With no hooks, `VsaShell` goes on a GameObject. The probe uses `ProbeShell : Shell<Vsa<UnityVsaHost>>` only to count the starts and stops in `OnStart` and `OnStop`.

### Parts of UnityVsaHost

Each part was removed in turn, and the checks ran again.

| Part | Without it | Runtime |
|---|---|---|
| `CreateEmptyApplicationBuilder` | EventLog logger fails (P3) | All |
| `ContentRootPath` | `DirectoryNotFoundException`: folder of `Unity.exe` | All |
| `Run => 0` | The default `Run` blocks the main thread | All |
| `Stop` override | No `StopAsync`, so `OnStop` does not run | All |
| Main thread `Dispatch` | Presenter events run on a pool thread | All |
| `UnityLogProvider` | No log in the Unity console | All |

The application name, the environment name and the command-line configuration are not necessary. The environment name is `Production` in the Editor too.

`Run` does not call `StartAsync`, so no custom `IHostLifetime` is necessary. With `StartAsync`, the default `ConsoleLifetime` adds a Ctrl+C handler, and IL2CPP cannot marshal it (`NotSupportedException`, `MonoPInvokeCallback`). The cost: `IHostedService` instances do not start, and `ApplicationStarted` does not fire. No Umpire part of the probe uses them.

| Check | Why | Play Mode | Mono player | IL2CPP player |
|---|---|---|---|---|
| `on-start` | `Shell.Awake` runs the app, `OnStart` runs | ok | ok | ok |
| `shell-running` | `IsRunning`, `Shell.Services` | ok | ok | ok |
| `shell-environment` | Environment name, content root | ok | ok | ok |
| `shell-request` | The await comes back on the main thread | ok | ok | ok |
| `shell-presenter-event` | Request from a pool thread, event on the main thread | ok | ok | ok |
| `shell-logger` | `ILogger` writes to the Unity console | ok | ok | ok |
| `shell-stop` | `Stop` calls `OnStop`, `Run` starts again | ok | ok | ok |
| Last `on-stop` | Quit or Play Mode exit stops the app | ok | ok | ok |

The players exit with the number of failed checks: 0 on Mono, 2 on IL2CPP (P2). Play Mode ran in batch mode with `-executeMethod Probe.Editor.ProbeEditor.Play`.

## Problems

### P1: Vsa<THost> is not available from the other packages

| Item | Value |
|---|---|
| When | First compile in Unity |
| Error | `CS0246: The type or namespace name 'Vsa<>' could not be found` |
| Where | `Assets/Probe/ProbeApp.cs`, `App<Vsa<VsaHost>>` |
| Why | `Vsa<THost>` is in the package `Umpire.Vsa`. `Umpire.Vsa.Composition` does not depend on it, so a vendor project without a direct `Umpire.Vsa` reference does not get the DLL. |
| Workaround in the probe | A direct `PackageReference` to `Umpire.Vsa` |

### P2: Microsoft DI refuses open generic services over structs on IL2CPP

| Item | Value |
|---|---|
| When | IL2CPP player only. Mono passes. |
| Check | `di-open-generic:Score`, `di-open-generic:Int32` |
| Error | `InvalidOperationException: Unable to create a generic service for type 'Probe.Lab.IBox`1[System.Int32]' because 'System.Int32' is a ValueType. Native code to support creating generic services might not be available with native AOT.` |
| Where | `CallSiteFactory.TryCreateOpenGeneric` (Microsoft.Extensions.DependencyInjection 8.0) |
| Why | DI checks `RuntimeFeature.IsDynamicCodeSupported`. IL2CPP gives `false`, so DI refuses each value type argument. The refusal is a DI rule, not an IL2CPP failure: Umpire's own `MakeGenericType` over the same structs works (`open-class:Score`). |
| Scope | Each `services.Add(typeof(X<>), typeof(Y<>))` that a struct closes. Umpire's closed registrations do not have this problem. |
| Switch | None. The check is compiled out only in the `netstandard2.0` build of the DLL. The probe vendors the `netstandard2.1` build. |

Closed registrations (`preview.2`, Mono and IL2CPP players, stripping Minimal):

| Check | Registration | Mono | IL2CPP |
|---|---|---|---|
| `di-open-generic:Score` | `AddSingleton(typeof(IBox<>), typeof(Box<>))` | ok | FAIL |
| `di-closed-generic:Score` | `AddSingleton<IBox<Score>, Box<Score>>()` | ok | ok |
| `di-closed-generic:Int32` | `AddSingleton<IBox<int>, Box<int>>()` | ok | ok |
| `di-open-plus-closed:Score` | The open registration, then the closed `IBox<Score>` registration | ok | ok |

Umpire registrations of an open interface (`[AutoRegister]` on `IShelf<T>`, `Shelf<T> : IShelf<T>`):

| Check | Resolve | Mono | IL2CPP |
|---|---|---|---|
| `auto-interface:Score` | `IShelf<Score>`, `Score` has `[TypeArgument]` | ok | ok |
| `auto-interface:Int32` | `IShelf<int>`, `[assembly: TypeArgument(typeof(int))]` | ok | ok |
| `auto-unlisted:FooBar` | `IShelf<FooBar>`, `FooBar` has no `[TypeArgument]` | not registered | not registered |

Umpire closes an open `[AutoRegister]` type only over the `[TypeArgument]` types. It makes no open DI registration, so P2 does not apply, and a type with no `[TypeArgument]` is not registered.

A mix of a plain DI open registration and Umpire closed registrations: DI uses an exact closed registration first and the open registration only as a fallback. So a marked struct takes the closed path and works on IL2CPP. A class takes the open path and works. A struct with no `[TypeArgument]` takes the open path and fails on IL2CPP only. This follows from `di-open-plus-closed:Score` and the DI source. It was not tested with an Umpire closed registration.

Lists (`IEnumerable<service>`) with an open and a closed registration for the same service:

| Check | Registrations | Mono | IL2CPP |
|---|---|---|---|
| `list-open-plus-closed:Score` | Open `IBox<>`, closed `IBox<Score>` | 2 items | FAIL, struct guard |
| `list-open-plus-closed:Article` | Open `IBox<>`, closed `IBox<Article>` | 2 items | 2 items |
| `list-open:FooBar` | Open `IBox<>` only | 1 item | 1 item |
| `list-umpire-plus-open:Score` | Umpire's closed `IShelf<Score>`, then open `IShelf<>` | 2 items | FAIL, struct guard |
| `list-open-pair:Article+Score` | Open `IPair<,>` only | 1 item | FAIL, struct guard |
| `list-open-pair:Article+Article` | Open `IPair<,>` only | 1 item | 1 item |
| `list-open-nested:List<Score>` | Open `IBox<>` only | 1 item | 1 item |
| `make-generic:Loose` | No DI. `MakeGenericType` and `Activator.CreateInstance` of `Box<Loose>`; no code names `Box<Loose>` | Works | Works |

For a list, DI adds each exact match and then each open match. So an open registration next to Umpire's closed registrations gives each item two times, and on IL2CPP the open pass throws for a struct, also a marked struct. Umpire resolves event handlers, hooks and presenters as lists, so an open fallback in `AutoRegistration` would run a handler two times on Mono and .NET, and break the publish of a struct event on IL2CPP.

The guard examines each direct type argument. One struct argument of two is enough to fail. A struct inside a class argument (`List<Score>`) passes. `make-generic:Loose` shows that IL2CPP in Unity 6000.6 can close and create a generic type over a struct that no code names. So on Unity the guard is stricter than the runtime.

### P3: The .NET Generic Host does not start in Unity

| Item | Value |
|---|---|
| When | Mono and IL2CPP |
| Check | `generic-host` (`new ProbeApp().Run(args)` with `VsaHost`) |
| Error | `PlatformNotSupportedException: EventLog access is not supported on this platform.` |
| Call stack | `EventLog..ctor` ← `EventLogLoggerProvider.CreateLogger` ← `LoggerFactory.CreateLogger` ← DI constructor call |
| Why | `Host.CreateApplicationBuilder` adds the Windows EventLog logger on Windows. The Unity class library does not support `System.Diagnostics.EventLog`. |
| Scope | Each app that uses `App.Run` with the default `CreateBuilder`. `UnityVsaHost` replaces `CreateBuilder` with `Host.CreateEmptyApplicationBuilder`, and does not have this problem. |

### P4: The IL2CPP player does not exit after P3

| Item | Value |
|---|---|
| When | IL2CPP player, after the `generic-host` check fails |
| Result | All checks finish, `Application.Quit` runs, the log shows the shutdown, but the process stays. Stopped by PID after 60 s. |
| Isolation | The same build with `-probeSkip generic-host` exits with code 2 (two failed checks). |
| Why | Not known. The failed host build leaves something that keeps the process alive. Possible sources: the console logger thread, the file watcher of `appsettings.json`. Not examined. |

### Observation: stripping Minimal changes the Microsoft.Extensions DLLs

Minimal keeps the Umpire DLLs complete, but it removes types from the Microsoft DLLs. No check failed from this, but each later stripping test must start from this fact.

| DLL | Types before → after | Methods before → after |
|---|---|---|
| `Umpire.Kernel` | 62 → 62 | 340 → 340 |
| `Umpire.Messages.Handlers` | 53 → 53 | 240 → 240 |
| `Umpire.Markers` | 11 → 11 | 13 → 13 |
| `Microsoft.Extensions.DependencyInjection` | 82 → 71 | 459 → 363 |
| `Microsoft.Extensions.Hosting` | 85 → 54 | 363 → 185 |

Source: `Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped/`, read with `System.Reflection.Metadata`.

## Umpire 0.2.0-preview.2 (2026-11-18)

The probe now uses `0.2.0-preview.2` (vsa-template `main` 88de76b). The sections above describe `preview.1`.

| Item | preview.1 | preview.2 |
|---|---|---|
| Vendored DLLs | 48 | 27. No `Microsoft.Extensions.Hosting` tree. |
| Unity host | `UnityVsaHost : VsaHost`: `CreateBuilder`, `Install`, `Run`, `Stop` | `UnityHost : AppHost`: `Configure` only |
| Shell | `App.Run`, `App.Stop` | `App.Start`, `App.Stop` |
| Probe compose | `CreateHost().Compose(services, app)` | `CreateHost().Compose(services, configuration, kernel, types)` |

| Run | Result |
|---|---|
| Editor batch (Mono) | All checks pass |
| Play Mode | All checks pass, also `shell-no-hosting` |
| Mono player, stripping Minimal | Only `settings` fails |
| Mono player, stripping Disabled | All checks pass |
| IL2CPP player, stripping Minimal | `settings` fails. `di-open-generic:Score` and `:Int32` fail (P2). |

P1, P3 and P4 do not occur. P2 does not change.

### P5: stripping Minimal breaks the dictionary bind of module settings

| Item | Detail |
|---|---|
| What | `LabSettings.Weights` (`Dictionary<string, int>`) is empty after the bind. The other settings bind. |
| Error | `KeyNotFoundException: The given key 'x' was not present in the dictionary.` |
| When | Mono and IL2CPP players with stripping Minimal. Not in the Editor, not with stripping Disabled. |
| Why | Minimal removes `IDictionary<TKey,TValue>.set_Item` from `mscorlib`. The binder sets dictionary values only through reflection on that setter, so the linker sees no use. The binder catches the exception and adds nothing. |
| Why `preview.1` passed | `Microsoft.Extensions.Configuration.FileExtensions.dll` calls `IDictionary<,>.set_Item`, and only `preview.1` vendors it (Hosting tree). Probably this call kept the setter. A `preview.1` Minimal `mscorlib` was not available to confirm it. |

How the cause was found:

1. The stripped DLLs of the Mono players, Minimal and Disabled, were compared with `System.Reflection.Metadata`.
2. Minimal keeps `IDictionary<,>.get_Item` and the `Item` property, but not `set_Item`. `Dictionary<,>.set_Item` stays.
3. `ConfigurationBinder.BindInstance` (8.0, line 410) gives the `IDictionary<,>` interface type to `BindDictionary`.
4. `BindDictionary` calls `indexerProperty.SetValue` on the interface `Item` property. Its `catch` throws only if `ErrorOnUnknownConfiguration` is set.
5. The vendored DLLs that reference `IDictionary<,>.set_Item`: `preview.1` has `Configuration`, `Configuration.FileExtensions` and `ComponentModel.Annotations`. `preview.2` has `Configuration` and `ComponentModel.Annotations`.

Test of the cause: `Assets/Probe/Lab/DictionaryUse.cs` has one method that no code calls. It sets an item on an `IDictionary<string, string>`. There is no `link.xml` and no `[Preserve]`. With this file, `settings` passes in the Mono and IL2CPP players with stripping Minimal. The type arguments do not need to match (`<string, string>` keeps the setter for `<string, int>`). The IL2CPP player still has only the two P2 failures.

## DI `netstandard2.0` build

Result: the `netstandard2.0` build of `Microsoft.Extensions.DependencyInjection` 8.0.1 removes the P2 guard on IL2CPP. All five P2 checks pass. But one new check fails: `validator`. This build is not a fix for P2.

| DLL in `Assets/Plugins/Umpire/` | Change |
|---|---|
| `Microsoft.Extensions.DependencyInjection.dll` | `netstandard2.1` (96,040 bytes) replaced by `netstandard2.0` (91,432 bytes). Same `.meta`. |
| Other DLLs | None added. Unity reported no missing reference. |

Checks that changed (stripping Minimal, code generation OptimizeSpeed):

| Check | Mono, `ns2.1` | Mono, `ns2.0` | IL2CPP, `ns2.1` | IL2CPP, `ns2.0` |
|---|---|---|---|---|
| `di-open-generic:Score` | ok | ok | FAIL (P2) | ok |
| `di-open-generic:Int32` | ok | ok | FAIL (P2) | ok |
| `list-open-plus-closed:Score` | ok | ok | FAIL (P2) | ok |
| `list-umpire-plus-open:Score` | ok | ok | FAIL (P2) | ok |
| `list-open-pair:Article+Score` | ok | ok | FAIL (P2) | ok |
| `validator` | ok | ok | ok | FAIL |

All other checks do not change. The IL2CPP player exits with code 1 because of `validator`. A second run of the same player gives the same failure.

The `validator` failure:

| Item | Detail |
|---|---|
| Error | `ArgumentException: An item with the same key has already been added. Key: Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceCacheKey` |
| Where | `scoped.GetRequiredService<T>()` in `LabChecks.Validator`. The `Dictionary.Add` call is in a `System.Linq.Expressions.Interpreter` frame, below `ServiceProvider.GetService`. |
| Probable cause | The `netstandard2.0` build has no IL emit resolver. It compiles the resolver with `Expression` trees, and IL2CPP runs them in the expression interpreter. The interpreted scoped-cache code adds a key that the scope already has. This cause is not confirmed. |

Other differences:

| Item | `ns2.1` | `ns2.0` |
|---|---|---|
| IL2CPP `GameAssembly.dll` | 11,388,928 bytes | 11,935,744 bytes (+546,816, +4.8 %) |
| Mono stripped DI DLL | 72,192 bytes | 71,680 bytes |
| Build warnings, IL2CPP | 2 | 2 |
| Build warnings, Mono | 6 | 4 |
| Build time, IL2CPP | Not recorded | 31 s |

The probe is restored: the `netstandard2.1` DLL is back in `Assets/Plugins/Umpire/` (MD5 `15148a168f8fd8514dc810e87dc2d210`). The `Builds/` and `Results/` files for `Mono2x-Minimal-OptimizeSpeed` and `IL2CPP-Minimal-OptimizeSpeed` are from the `netstandard2.0` runs.

## VContainer 1.19.0, no Umpire

`Assets/Probe/Lab/VContainerChecks.cs` runs the DI lab cases with VContainer only: `ContainerBuilder`, `Register(typeof(Box<>)).As(typeof(IBox<>))`, then `Resolve`. Stripping Minimal, code generation OptimizeSpeed. Mono and IL2CPP give the same results.

| Check | Registration | Resolve | Mono | IL2CPP |
|---|---|---|---|---|
| `vc-open:Article` | Open | `IBox<Article>` | Works | Works |
| `vc-open:Score` | Open | `IBox<Score>` | Works | Works |
| `vc-open:Int32` | Open | `IBox<int>` | Works | Works |
| `vc-open:Loose` | Open | `IBox<Loose>`, no closed use in code | Works | Works |
| `vc-closed:Score` | Closed | `IBox<Score>` | Works | Works |
| `vc-open-plus-closed:Score` | Open and closed | `IBox<Score>` | Works | Works |
| `vc-list-open-plus-closed:Score` | Open and closed | List of `IBox<Score>` | 1 item | 1 item |
| `vc-list-open-plus-closed:Article` | Open and closed | List of `IBox<Article>` | 1 item | 1 item |
| `vc-list-open:FooBar` | Open | List of `IBox<FooBar>` | **0 items** | **0 items** |
| `vc-list-open:Score` | Open | List of `IBox<Score>` | **0 items** | **0 items** |
| `vc-open-pair:Article+Score` | Open `IPair<,>` | `IPair<Article, Score>` | Works | Works |
| `vc-list-open-pair:Article+Score` | Open `IPair<,>` | List of `IPair<Article, Score>` | **0 items** | **0 items** |
| `vc-nested:List<Score>` | Open `ListBox<>`, `AsImplementedInterfaces` | `IBox<List<Score>>` | **Wrong instance** | **Wrong instance** |

- VContainer has no struct guard. Open registrations over structs work on IL2CPP.
- A list resolve does not include open registrations. A single resolve of `IBox<FooBar>` works, but the list of `IBox<FooBar>` is empty.
- No duplicates: with an open and a closed registration, the list has only the closed item.
- Nested shape: VContainer closes `ListBox<>` over the service argument `List<Score>`. It returns a `ListBox<List<Score>>`, which is not an `IBox<List<Score>>`. `Resolve(Type)` gives no error.
- The IL2CPP player exits with code 5 because of the five Microsoft DI checks of P2. The VContainer checks all ran.
- The VContainer source confirms the API: `Register(Type)` uses `OpenGenericRegistrationBuilder`, and a list reads exact registrations only (`Registry.TryFallbackToSingleElementCollection`). The docs have no page for open generics.

Several implementations of one service. Mono and IL2CPP give the same results.

| Check | Registrations | Single resolve | List resolve |
|---|---|---|---|
| `vc-multi-closed:Score` | `Box<Score>`, `OtherBox<Score>`, closed | `OtherBox` (last) | `Box`, `OtherBox` |
| `vc-same-twice:Score` | `Box<Score>` two times, closed | **Build fails** | **Build fails** |
| `vc-multi-open:Score` | `Box<>`, `OtherBox<>`, open | `OtherBox` (last) | **Empty** |
| `di-multi-closed:Score` | Microsoft DI, as `vc-multi-closed` | `OtherBox` (last) | `Box`, `OtherBox` |
| `di-same-twice:Score` | Microsoft DI, as `vc-same-twice` | `Box` | `Box`, `Box` |

`vc-same-twice` fails at `builder.Build()`: `VContainerException: Conflict implementation type : Registration Box`1`. VContainer refuses one implementation type two times for one service.

## Umpire 0.2.0-preview.3 (2026-11-18)

vsa-template `main` at `6c28156`, tag `v0.2.0-preview.3`. Stripping Minimal, code generation OptimizeSpeed.

| Item | Result |
|---|---|
| Vendored DLLs | 26 (preview.2: 27). Out: `Microsoft.Extensions.Configuration.Binder`, `System.Buffers`, `System.Memory`, `System.Numerics.Vectors`. In: `Tomlyn`, `Umpire.Config`, `System.IO.Pipelines`. `System.Text.Json` and `System.Collections.Immutable` come from Unity 6. |
| Settings source | `UnityHost.Settings(types)` asks the `ModuleSettingsAsset` assets on the shell first (`AssetSettings`), then each `UnitySettings` class of the app, in name order. It does not read `.umpireconfig`. |
| Settings asset | `ModuleSettingsAsset<TModule, TSettings>`: a `ScriptableObject` with Unity fields and a `Create()` method. Unity cannot serialize a `Dictionary`, so `LabSettingsAsset` keeps the weights as a list of pairs. `ProbeEditor.SaveScene` writes `Assets/Probe/Lab/LabSettings.asset` and puts it on the shell. |
| `DictionaryUse.cs` | Removed |

| Check | Mono | IL2CPP | Note |
|---|---|---|---|
| All app, shell and lifecycle checks (start, stop, start again) | Pass | Pass | No change from preview.2 |
| `settings`: the compose of `UnityHost`. The `LabSettingsAsset` on the shell wins over `ProbeSettings`. | Pass | Pass | `asset 9 b y=3 On` |
| `settings-code`: `ProbeSettings` (inline data, a dictionary included) | Pass | Pass | `hello 7 a x=2 On` |
| `settings-toml`: the same data from TOML text through `UmpireConfig.Parse(text).Get<LabSettings>("Lab")` | Pass | Pass | Tomlyn reflection. The dictionary binds with no `DictionaryUse.cs`. |
| `settings-toml-case`: the key `greeting` for the property `Greeting` | Pass | Pass | It binds: `Greeting = lower`. `UmpireConfig` uses `PropertyNameCaseInsensitive = true`. |

- P5 does not occur with preview.3. The binder is gone, and Tomlyn binds the dictionary on IL2CPP at stripping Minimal.
- Tomlyn loads with the `System.Text.Json` copy of Unity 6 on Mono and on IL2CPP.
- The only failed checks are the container cases that fail by design: the D and V cases of P2, and the Microsoft DI and VContainer cases of `Probe/Assets/Spike/`.
- Not tested: the `TomlSerializerContext` path, and a `.umpireconfig` file in a build.

## Not tested

- Stripping Low, Medium and High, `link.xml`, and the stripping attributes (`[Preserve]`, `[RequireDerived]`, `[RequireImplementors]`). Minimal does not strip the Umpire DLLs, so these attributes had no effect in these tests.
- IL2CPP code generation `OptimizeSize`.
- The analyzers and the rule values (`.globalconfig`).
- Modules in asmdefs, and module source outside `Assets/`.
- A domain reload during Play Mode (script change while the app runs).
- Android and iOS.

## Open questions

- P2: does Umpire or an app need open generic DI registrations over structs in Unity?
- P4: what keeps the player process alive?
