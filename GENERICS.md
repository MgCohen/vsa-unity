# Open generics in Umpire: limits and requirements


> Since Umpire 0.2.0-preview.12, `[TypeArgument]` is named `[AutoClose]`. The sections below keep the old name where they describe older results.
Umpire uses open generic classes for handlers, hooks, validators and services. It does not register open presenters (`PresenterServiceCollectionExtensions.cs:26`). Microsoft DI has two limits for them, and the Unity linker adds a third. The Umpire scan closes the generics itself to avoid the DI limits. This document lists the limits, the uses, the current solution, the other solutions, and the requirements for a final solution.

Test data: [Probe/RESULTS.md](Probe/RESULTS.md) (Umpire `0.2.0-preview.2`, Unity 6000.6.6f1, Microsoft DI 8.0.1).

## 1. The limits

L1 and L2 are in Microsoft DI. Unity does not cause L2, but IL2CPP sets the flag that starts it. L3 is in the Unity linker.

| Limit | Where | Platforms | Example that fails |
|---|---|---|---|
| L1. One open shape only | DI open registration | All | `IHandler<Changed<T>>` → `ChangedHandler<T>` |
| L2. No struct type arguments | DI open registration, at resolve | IL2CPP, .NET NativeAOT | `IBox<>` → `Box<>`, resolve `IBox<Score>` |
| L3. Stripping removes reflection-only code | Unity linker, at build | Unity players, Mono and IL2CPP | A handler that only the scan finds |

### L1: one open shape only

A DI open registration maps a service to an implementation with the same type parameters, in the same order.

```csharp
services.AddSingleton(typeof(IStore<>), typeof(Store<>));   // works
// ChangedHandler<T> : IHandler<Changed<T>>                  // no open registration can express it
```

Umpire bases put the type parameter inside another type, for example `CommandHandler<ChangeResourceCommand<T>, int>`. So DI cannot register them open on any platform.

### L2: no struct type arguments

If `RuntimeFeature.IsDynamicCodeSupported` is false, DI refuses to close an open registration over a struct. IL2CPP reports false.

The guard examines only the direct type arguments. Section 2 gives each case.

The Unity runtime can do the work. `MakeGenericType` over a struct that no code names works on IL2CPP (`make-generic:Loose`). So the limit is the DI guard, not the runtime.

No switch stops the guard. The `netstandard2.0` build of DI has no guard, but on IL2CPP it breaks a normal scoped resolve (`validator`). The guard and the safe resolver engine come from the same flag.

### L3: stripping removes reflection-only code

The Unity linker removes each type and member that no code names. The scan and `ActivatorUtilities` find types by reflection, so the linker cannot see these uses.

| Stripping level | Removes | Effect on Umpire |
|---|---|---|
| Disabled | Nothing | None |
| Minimal | Class libraries only (`mscorlib`, `System.*`) | `IDictionary<,>.set_Item`: the settings bind fails (P5) |
| Low, Medium, High | Also code in `Assets/` and the plugin DLLs | Not tested. Umpire puts `[RequireDerived]` and `[Preserve]` on its bases. |

Stripping removes two kinds of code:

| Kind | Example | Who uses it by reflection | Fix that works |
|---|---|---|---|
| Types | A handler that only the scan finds | The Umpire scan, `ActivatorUtilities` | Markers on the bases, or generated code |
| Members | `IDictionary<,>.set_Item` (P5) | `ConfigurationBinder`, not Umpire | One call in code: `Probe/Assets/Probe/Lab/DictionaryUse.cs` |

The member kind is not a generics problem, but it has the same cause. A marker on an Umpire base cannot keep a member of `mscorlib`. Only a call in code or a `link.xml` entry keeps it.

A consumer type that is not below a marked base needs its own marker or a `link.xml` entry. The linker gives no error. The type or member is missing at run time.

## 2. The cases

"Editor, Mono" is the Editor Play Mode and the Mono player: their results are the same. "IL2CPP" is the IL2CPP player. Each check name is in `Probe/Assets/Probe/Lab/LabChecks.cs`.

<details>
<summary>Plain DI registrations · 12 cases, 4 fail on IL2CPP</summary>

`IBox<>` → `Box<>` and `IPair<,>` → `Pair<,>` are open registrations. `Article` and `FooBar` are classes. `Score` and `int` are structs. Only `FooBar` has no `[TypeArgument]`, but plain DI does not read the marker.

| Case | Registration | Resolve | Editor, Mono | IL2CPP | Check |
|---|---|---|---|---|---|
| D1 | Open | `IBox<Article>` | Works | Works | `di-open-generic:Article` |
| D2 | Open | `IBox<Score>` | Works | **Fails** | `di-open-generic:Score` |
| D3 | Open | `IBox<int>` | Works | **Fails** | `di-open-generic:Int32` |
| D4 | Closed | `IBox<Score>`, `IBox<int>` | Works | Works | `di-closed-generic:*` |
| D5 | Open and closed | `IBox<Score>` | Works | Works | `di-open-plus-closed:Score` |
| D6 | Open and closed | List of `IBox<Score>` | **2 items** | **Fails** | `list-open-plus-closed:Score` |
| D7 | Open and closed | List of `IBox<Article>` | **2 items** | **2 items** | `list-open-plus-closed:Article` |
| D8 | Open | List of `IBox<FooBar>` | 1 item | 1 item | `list-open:FooBar` |
| D9 | Open | List of `IPair<Article, Score>` | 1 item | **Fails** | `list-open-pair:Article+Score` |
| D10 | Open | List of `IPair<Article, Article>` | 1 item | 1 item | `list-open-pair:Article+Article` |
| D11 | Open | List of `IBox<List<Score>>` | 1 item | 1 item | `list-open-nested:List<Score>` |
| D12 | Open, nested shape | `IHandler<Changed<Score>>` | Not possible | Not possible | None. The DI API cannot express it. |

</details>

<details>
<summary>Umpire registrations · 10 cases, 1 fails on IL2CPP</summary>

`Store<T>`, `ChangedHandler<T>` and `IShelf<T>` have `[AutoRegister]`. Umpire registers each one closed.

| Case | Type argument | Resolve | Editor, Mono | IL2CPP | Check |
|---|---|---|---|---|---|
| U1 | `Article`, `[TypeArgument]` | `Store<Article>` | Works | Works | `open-class:Article` |
| U2 | `Score`, `[TypeArgument]` | `Store<Score>` | Works | Works | `open-class:Score` |
| U3 | `int`, assembly `[TypeArgument]` | `Store<int>` | Works | Works | `open-class:Int32` |
| U4 | `Score`, `[TypeArgument]` | Publish `Changed<Score>` | Works | Works | `open-handler:Score` |
| U5 | `Score`, `[TypeArgument]` | `IShelf<Score>` | Works | Works | `auto-interface:Score` |
| U6 | `FooBar`, no marker | `IShelf<FooBar>` | Not registered | Not registered | `auto-unlisted:FooBar` |
| U7 | `Score`, plus a consumer open `IShelf<>` | List of `IShelf<Score>` | **2 items** | **Fails** | `list-umpire-plus-open:Score` |
| U8 | `FooBar`, plus a consumer open `IShelf<>` | `IShelf<FooBar>` | Works, from D8 | Works, from D8 | None. Not tested. |
| U9 | Two type parameters | Start | Start stops | Start stops | None. From the Umpire docs. |
| U10 | No marker, send a generic command | Send | "has no handler" | "has no handler" | None. From the Umpire docs. |

</details>

<details>
<summary>The runtime, with no DI · 2 cases</summary>

| Case | Action | Editor, Mono | IL2CPP | Check |
|---|---|---|---|---|
| R1 | `MakeGenericType` of `Box<>` over `Loose`, a struct that no code names | Works | Works | `make-generic:Loose` |
| R2 | The same over a struct, on .NET NativeAOT | Not tested | Not applicable | None |

</details>

<details>
<summary>VContainer 1.19.0, no Umpire · 13 cases, the same on Mono and IL2CPP</summary>

| Case | Registration | Resolve | Mono and IL2CPP | Check |
|---|---|---|---|---|
| V1 | Open | `IBox<Article>` | Works | `vc-open:Article` |
| V2 | Open | `IBox<Score>`, `IBox<int>`, `IBox<Loose>` | Works | `vc-open:*` |
| V3 | Closed | `IBox<Score>` | Works | `vc-closed:Score` |
| V4 | Open and closed | `IBox<Score>` | Works | `vc-open-plus-closed:Score` |
| V5 | Open and closed | List of `IBox<Score>`, `IBox<Article>` | 1 item, the closed one | `vc-list-open-plus-closed:*` |
| V6 | Open | List of `IBox<FooBar>`, `IBox<Score>` | **0 items** | `vc-list-open:*` |
| V7 | Open `IPair<,>` | `IPair<Article, Score>` | Works | `vc-open-pair:Article+Score` |
| V8 | Open `IPair<,>` | List of `IPair<Article, Score>` | **0 items** | `vc-list-open-pair:Article+Score` |
| V9 | Open `ListBox<>` (nested shape) | `IBox<List<Score>>` | **Wrong instance**, no error | `vc-nested:List<Score>` |
| V10 | Two closed implementations | One and list | One: the last. List: both. | `vc-multi-closed:Score` |
| V11 | One closed implementation two times | Build | **Build fails**: "Conflict implementation type" | `vc-same-twice:Score` |
| V12 | Two open implementations | One and list | One: the last. List: **empty**. | `vc-multi-open:Score` |

</details>

### What the cases show

- An undeclared type (U6, U10) fails the same way on all platforms. This is the cost of the marker.
- IL2CPP adds failures only where an open registration closes over a struct (D2, D3, D6, D9, U7).
- An open registration next to a closed one gives duplicates on all platforms (D6, D7, U7).
- A consumer open registration hides a missing marker for classes only (U8). For structs it fails on IL2CPP only.
- VContainer has no struct guard (V2), but its lists do not include open registrations (V6, V8). A single resolve and a list resolve then disagree.
- VContainer does not match nested shapes. It returns a wrong instance with no error (V9).
- With closed registrations, VContainer and Microsoft DI agree: the single resolve gives the last item, and the list gives all (V10). VContainer refuses one implementation type two times for one service (V11). Microsoft DI accepts it.

### Samples

The types of the samples:

```csharp
public sealed class Article { }               // a class
public struct Score { public int Value; }     // a struct
public sealed class FooBar { }                // a class with no [TypeArgument]
```

**Plain DI: works on all platforms**

```csharp
services.AddSingleton(typeof(IBox<>), typeof(Box<>));
provider.GetRequiredService<IBox<Article>>();                // D1: class argument

services.AddSingleton<IBox<Score>, Box<Score>>();
provider.GetRequiredService<IBox<Score>>();                  // D4: closed registration
```

**Plain DI: fails on IL2CPP only**

```csharp
services.AddSingleton(typeof(IBox<>), typeof(Box<>));
provider.GetRequiredService<IBox<Score>>();                  // D2: struct argument
provider.GetRequiredService<IEnumerable<IPair<Article, Score>>>();   // D9: one struct of two
// InvalidOperationException: Unable to create a generic service for type
// 'IBox`1[Score]' because 'Score' is a ValueType.
```

**Plain DI: duplicates on all platforms, and fails on IL2CPP for a struct**

```csharp
services.AddSingleton(typeof(IBox<>), typeof(Box<>));
services.AddSingleton<IBox<Score>, Box<Score>>();
provider.GetRequiredService<IBox<Score>>();                  // D5: works, 1 instance
provider.GetServices<IBox<Score>>();                         // D6: 2 items; IL2CPP fails
```

**Plain DI: not possible on any platform**

```csharp
public sealed class ChangedHandler<T> : IHandler<Changed<T>> { }
// D12: no open registration maps IHandler<Changed<T>> to ChangedHandler<T>.
services.AddSingleton(typeof(IHandler<>), typeof(ChangedHandler<>));   // wrong: maps IHandler<T>, not IHandler<Changed<T>>
```

**Umpire: works on all platforms**

```csharp
[TypeArgument] public struct Score { public int Value; }

[AutoRegister(Lifetime.Singleton)]
public sealed class Store<T> { }                             // U2: Store<Score> is registered

public sealed class ChangedHandler<T> : IntegrationEventHandler<Changed<T>> { }
await events.PublishAsync(new Changed<Score>(score));        // U4: the handler runs
```

**Umpire: fails on all platforms (no marker)**

```csharp
public sealed class FooBar { }                               // no [TypeArgument]

provider.GetService<IShelf<FooBar>>();                       // U6: null
await sender.SendAsync(new ChangeResourceCommand<FooBar>()); // U10: "has no handler" (Umpire docs)
```

**Umpire plus a consumer open registration**

```csharp
// The consumer adds an open fallback after the Umpire compose.
services.AddSingleton(typeof(IShelf<>), typeof(Shelf<>));
provider.GetService<IShelf<FooBar>>();                       // U8: works; hides the missing marker (not tested)
provider.GetServices<IShelf<Score>>();                       // U7: 2 items; IL2CPP fails
```

## 3. The uses

| Use | Example | Base with `[AutoRegister]` |
|---|---|---|
| A command for each resource | `ChangeResourceHandler<T>` for `Health`, `Mana` | `CommandHandler<,>` |
| A request for each type | `GetHandler<T>` | `RequestHandler<,>` |
| An event for each type | `ChangedHandler<T>` for `Changed<T>` | `IntegrationEventHandler<>` |
| A rule for each type | `DamageRule<T>` for `Sword`, `Dagger` | An app interface |
| A validator for each type | `RangeValidator<T>` | `Validator<>` |
| A plain service for each type | `Store<T>` | `[AutoRegister]` on the class |

The goal is one generic class for a family of types, with no copy for each type. Game code often uses structs for these types (`Health`, `Score`), so L2 matters most in Unity.

The pipeline does not resolve handlers, hooks, validators or presenters as DI lists. It indexes the closed types from `AppTypes.Implementations()`, and it creates each one with `ActivatorUtilities.GetServiceOrCreateInstance(scope, closedType)` (vsa-template `88de76b`, `framework/Components/Messages/Handlers/Pipeline/PipelineRun.cs:94` and `Pipeline.cs`). Umpire uses DI lists only for non-generic services (`Operation`, `IIntegrationEventObserver`). So the list cases of section 2 apply only to app code that resolves `IEnumerable<ISomething<T>>`.

## 4. The current solution

The Umpire scan closes each open `[AutoRegister]` class at start, and registers only closed types.

```mermaid
flowchart LR
    A[Scan finds open class] --> B[Scan finds TypeArgument types]
    B --> C[Filter by constraints]
    C --> D[MakeGenericType]
    D --> E[Closed DI registration]
```

Closed registrations avoid L1 and L2 (cases U1 to U5).

| Limit of the current solution | Effect |
|---|---|
| `[TypeArgument]` required | A type without the marker is never registered |
| One type parameter only | An open class with two parameters stops the start |
| Each class × each fitting argument | Combinations that no code uses (see below) |
| Missing marker | Error at use: "has no handler" |
| Consumer open registrations | L2 fails on IL2CPP only, not in the Editor |

The number of closed registrations is the product of the open classes and the fitting type arguments. Constraints are the only filter.

| Open classes | Fitting type arguments | Closed registrations |
|---|---|---|
| 5 | 4 | 20 |
| 10 | 10 | 100 |
| 20 | 30 | 600 |

Each one is a DI descriptor and a `MakeGenericType` call at start. On IL2CPP, a combination that no code names runs in shared generic code, so it does not add native code. It is slower than code made for that combination.

The reflection scan cannot read method bodies. So it cannot find `new Changed<Score>(...)` in code, and it needs the marker.

Do not add an open DI registration next to the closed ones as a fallback. A list then gets each item two times on all platforms, and fails on IL2CPP for a struct (case U7).

## 5. The solutions

| Solution | Removes marker | Avoids L1 | Avoids L2 | Status |
|---|---|---|---|---|
| Current: scan closes generics | No | Yes | Yes | In use |
| A. Close at dispatch time | Yes, for messages | Yes | Yes | Not tested |
| B. Source generator | Yes, for named uses | Yes | Yes | Not tested |
| C. VContainer | No | No: wrong instance | Single only | Tested |
| D. DI `netstandard2.0` build | No | No | Yes | Refused: breaks scoped resolves |
| E. Compose check | No | No | Shows L2 in Editor | Not tested |

### A. Close at dispatch time

The publisher gets a `Changed<Score>` instance. It finds the open handlers from the scan, closes them with `MakeGenericType`, creates them with `ActivatorUtilities`, and keeps them in a cache. Umpire then owns the handler lifetimes, not DI. It does not apply to plain services such as `Store<T>`.

### B. Source generator

A Roslyn generator reads the code at compile time and finds each closed use, for example `Changed<Score>`. It writes closed registrations as normal C# code. AOT compilers see the closed names and make their native code. Types that only reflection makes still need `[TypeArgument]`. The Unity path for a generator is not tested.

### C. Other DI container

VContainer 1.19.0 is tested (cases V1 to V9). It closes open registrations over structs on IL2CPP. But its lists do not include open registrations, and it returns a wrong instance for a nested shape. So it fails the list requirement, and it does not avoid L1. Autofac and DryIoc map nested open generics, from their documents. They are not tested.

### E. Compose check

At compose, Umpire finds each open registration whose service it closed over a struct. It then fails on all platforms. E is an addition to the other solutions, not a replacement.

### Stripping and the solutions

| Solution | Effect on L3 |
|---|---|
| Current | Markers on the Umpire bases. Not tested above Minimal. |
| A | Same as current: types found by reflection |
| B | Generated code names each type, so the linker keeps it. It can also write the member calls, for example `set_Item`. |
| C, D | No effect |
| E | No effect |

B is the only solution that removes the need for preserve markers and `link.xml`.

## 6. Requirements

The stripping, list and NativeAOT rows are decided. The other rows are a draft.

### Must

| Requirement | Reason |
|---|---|
| The same app code works in the Editor, IL2CPP and .NET | No platform code in modules |
| Umpire bases work over structs and classes | Game types are often structs |
| A failure shows in the Editor | A build takes minutes |
| Works at stripping Minimal | The level that most Unity projects use |
| A single resolve and a list resolve work for each registered service | App code resolves services and lists (`IEnumerable<IRule<T>>`) |
| A list resolve gives each item one time, with the same result on all platforms | No failure that shows only in a build |
| `IServiceCollection` stays the composition API | Consumers and hosts use it |

### Should

| Requirement | Reason |
|---|---|
| No marker for types that code names | Less to remember |
| Two or more type parameters | Some families need two types |
| Only used combinations are registered | Smaller start and build |
| No `link.xml` for consumer code | The linker gives no error |

### Acceptable limits

| Limit | Condition |
|---|---|
| A type that only reflection makes needs a marker | The error names the missing marker |
| A consumer open registration over a struct fails on IL2CPP | The compose check shows it in the Editor |
| Stripping Medium and High are not supported | The docs state the limit |

## 7. Decisions

### Close at compose

After the modules run `Configure`, Umpire replaces each open descriptor with closed descriptors. The container never gets an open type. Umpire 0.2.0-preview.12 ships it (MgCohen/vsa-template#266). The rules and the results are in the open generics guide (see `AGENTS.md`).

| Fact | Source |
|---|---|
| The closing prototype passes C1 to C3 on Mono, IL2CPP Minimal and IL2CPP High | `Probe/Assets/Spike/OpenClosing.cs`, checks `spike:C*:closing` |
| No source generator is necessary. A generator is a later option | Plan step 8 |

### Attributes

| Attribute | Decision | Reason |
|---|---|---|
| `[AutoRegister]` | Keep in the core | The core scan registers handlers, hooks, validators and services on each host |
| `[TypeArgument]` | Keep in the core, rename to `[AutoClose]` | The core scan closes open types over it on each host. The name pairs with `[AutoRegister]` |
| `[Preserve]` | Remove | No effect on Low and Minimal (spike S1 to S9). On High it keeps only the marked type |
| `[RequireDerived]`, `[RequireImplementors]` | Remove until the High review | No effect on Low and Minimal |

- One attribute is not used. It would also register each argument type, for example `Score`, as a service.
- Both attributes work on the type itself, on a base class at any depth, and on an interface (`MarkedTypes.Marker`).
- High stripping is out of scope until a review. Issue #223 tracks it.

## Non-goals

- No .NET NativeAOT support. Reason: no user needs it now.

## Open questions

- Are the draft rows correct: no marker, two type parameters, used combinations, no `link.xml`?
- Does Unity run a Roslyn generator for code in `Assets/` and in asmdefs, and on IL2CPP?
- Is a generic class with two type parameters necessary?
- Is solution A useful as a fallback to B for message dispatch?
