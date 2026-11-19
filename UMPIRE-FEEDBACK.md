# Feedback for Umpire: 0.2.0-preview.2 in Unity

The Unity probe runs Umpire `0.2.0-preview.2` (vsa-template `main` 88de76b) in Unity 6000.6.6f1. The Unity host is one `Configure` method, and the vendored DLLs went from 48 to 27. Two runtime problems, three design points and three packaging notes stay open. Full test data: [Probe/RESULTS.md](Probe/RESULTS.md).

## Results

| Run | Result |
|---|---|
| Editor batch (Mono), Play Mode | All checks pass |
| Mono player, stripping Disabled | All checks pass |
| Mono player, stripping Minimal | All checks pass, with the P5 test code |
| IL2CPP player, stripping Minimal | Only P2 fails, with the P5 test code |

Problems P1, P3 and P4 of `preview.1` do not occur.

## Runtime problems

### P2: Microsoft DI refuses open generic services over structs on IL2CPP

| Item | Detail |
|---|---|
| What | `services.AddSingleton(typeof(IBox<>), typeof(Box<>))`, then a resolve of `IBox<Score>` or `IBox<int>` |
| Error | `InvalidOperationException: Unable to create a generic service for type 'IBox`1[Score]' because 'Score' is a ValueType.` |
| When | IL2CPP only. Mono passes. |
| Scope | Plain DI registrations only. Umpire's own open classes and handlers over structs work. |
| Why | A guard in DI 8.0: if `RuntimeFeature.IsDynamicCodeSupported` is false, it refuses struct arguments before `MakeGenericType`. No switch turns it off. |
| Workaround | A closed registration works on IL2CPP: `AddSingleton<IBox<Score>, Box<Score>>()`. It also works after the open registration, for that struct only. |
| Umpire path | `[AutoRegister]` on the open class or interface, and `[TypeArgument]` on the struct. Umpire registers closed types, so the guard does not run. Tested on IL2CPP with an open interface (`IShelf<Score>`, `IShelf<int>`). A type with no `[TypeArgument]` is not registered on any platform. |
| Trap | A plain DI open registration as a fallback works for classes. A struct with no `[TypeArgument]` then fails on IL2CPP only, so the Editor does not show the failure. |
| Open fallback in `AutoRegistration` | Do not add one. Tested: with an open `IShelf<>` next to Umpire's closed `IShelf<Score>`, `IEnumerable<IShelf<Score>>` has 2 items on Mono and throws on IL2CPP. The pipeline does not use DI lists: it creates each closed handler, hook and validator with `ActivatorUtilities.GetServiceOrCreateInstance` (`PipelineRun.cs:94`). So the duplicate affects only app code that resolves `IEnumerable<IShelf<T>>`. |
| Exact rule | The resolve fails if DI must close an open registration and one or more direct type arguments are structs. `IPair<Article, Score>` fails. `IBox<List<Score>>` passes. |
| Runtime | IL2CPP in Unity 6000.6 can do `MakeGenericType` over a struct that no code names (`make-generic:Loose`). The guard is stricter than the Unity runtime. |
| Status | Not fixed. Low priority. |

### P5: stripping Minimal breaks the dictionary bind of module settings

| Item | Detail |
|---|---|
| What | A `Dictionary<string, int>` property of `Module<TSettings>` settings stays empty. Other settings bind. |
| When | Mono and IL2CPP players with stripping Minimal. Not in the Editor, not with stripping Disabled. |
| Why | Minimal removes `IDictionary<TKey,TValue>.set_Item` from `mscorlib`. `ConfigurationBinder` (8.0) calls that setter only through reflection on the interface, and its `catch` hides the error. |
| Why `preview.1` passed | Only `preview.1` vendors `Microsoft.Extensions.Configuration.FileExtensions.dll`, which calls `IDictionary<,>.set_Item`. Probably this call kept the setter. Not confirmed. |
| Test | One method in `Assets/` that no code calls, with no `link.xml` and no `[Preserve]`, makes `settings` pass in both players. The type arguments do not need to match. File: [Probe/Assets/Probe/Lab/DictionaryUse.cs](Probe/Assets/Probe/Lab/DictionaryUse.cs). |
| Risk | The binder uses reflection on other .NET members too. Higher stripping levels can remove more of them, and the bind gives no error. |

```csharp
static void Set() {
    IDictionary<string, string> values = new Dictionary<string, string>();
    values["a"] = "b";
}
```

## Design points

| Point | Now | Note |
|---|---|---|
| Generic nesting | An app with hooks writes `Shell<Vsa<UnityHost>>` | A `VsaShell<TArchitecture> : Shell<Vsa<TArchitecture, UnityHost>>` hides the host. This is a change in vsa-unity, not in Umpire. |
| Presenter dispatch | `LivePresenters.Dispatch` with `Awaitable.MainThreadAsync` in `UnityHost.Configure` | It works. It is the only line of the host that is not obvious. Proposal C4 (dispatch through the `SynchronizationContext` of `Connect`) would remove it. |
| `AppStop.Request` | Does nothing in Unity | Unity does not call `App.Run`, so nothing watches the request. The shell can watch it later. |

## Packaging

| Note | Detail |
|---|---|
| Private packages | `Umpire.Kernels`, `Umpire.Hosts.Web` and `Umpire.Hosts.Console` are private on GitHub Packages. A Unity user without owner access cannot restore them. |
| Operations off on `netstandard2.1` | `RequestOperations` needs static virtual members (`net10.0`). In Unity it registers nothing. No Unity check needs it now. |
| C# 9 in `Assets/` | App code needs an `IsExternalInit` polyfill. It cannot use file-scoped namespaces or primary constructors. |

## The Unity host now

```csharp
public sealed class UnityHost : AppHost {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        services.AddLogging(logging => logging.AddProvider(new UnityLogProvider()));
        services.AddSingleton<LivePresenters.Dispatch>(async work => {
            await Awaitable.MainThreadAsync();
            await work();
        });
    }
}
```

The shell calls `app.Start(arguments)` in `Awake` and `app.Stop()` in `OnDestroy`.

## Not tested

- Stripping Low, Medium and High, `link.xml`, and the stripping attributes
- IL2CPP code generation `OptimizeSize`
- The analyzers and `.globalconfig`
- Modules in asmdefs
- A domain reload while the app runs
- Android and iOS
