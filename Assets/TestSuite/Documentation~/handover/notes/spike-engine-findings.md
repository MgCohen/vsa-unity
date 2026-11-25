# Engine findings on the test suite spike (Umpire preview.24)

**Summary.** The Umpire engine runs on the vsa-unity project after a Unity compile. On the test suite spike it gives 42 errors. Each finding agrees with the VSA rules of preview.24, so the engine reports correctly. 27 findings have a small fix on our side. 15 findings need a design decision. Three groups can show template gaps (G3, G4, G7).

Source: PR #20 branch `claude/testsuite-core`, commit `667503c`. Engine: `engine/net8.0` from the vsa-template release `v0.2.0-preview.24`. Full output: `spike-engine-preview24.txt` in this folder.

## How the engine ran

```bash
dotnet umpire-engine.dll --project <project> --block Packages/com.vsa.testsuite/Spike --bin Library/ScriptAssemblies --stage Check
```

`.umpireconfig` in the block folder, from the "Rule engine" section of `Packages/com.umpire.unity/README.md`:

```toml
[umpire.check]
references = "<project>/Library/ScriptAssemblies, <project>/Packages/com.umpire.unity/Runtime/Plugins, <Editor>/Data/Managed/UnityEngine"
source_root = "<project>"
project_checks = false

[umpire.check.folder_aliases]
"" = ["", "App"]
```

| Block | Errors |
|---|---|
| `Assets/DiceGame` | 0 |
| `Packages/com.vsa.testsuite/Spike` | 42 |
| Spike with a rule break in `Touch/CliCommand/Touch.cs` (`typeof(TouchSystem)`) | 43: the new one is `Touch in CliCommand uses TouchSystem in Domain: no rule allows it` |

## Overview

| # | Root cause | Findings | Engine correct? | Fix |
|---|---|---|---|---|
| G1 | A Touch object slice sends the requests of other slices | 12 | Yes | Ours: use the domain, not the requests |
| G2 | Two slices share one result type | 8 | Yes | Ours: move the result to `Slices/Shared` |
| G3 | Domain and a handler use the module settings class | 4 | Yes, by the rules | Ours, or a template gap |
| G4 | The host calls the Session plugin | 1 | Yes | Decision, maybe a template gap |
| G5 | A feature domain uses the Session plugin contracts | 1 | Yes | Ours: move to Infrastructure |
| G6 | `ScriptContext` in Contracts sends requests | 3 | Yes | Decision: move to Presenters |
| G7 | `SessionBus` and `TraceDoneStep` in Infrastructure send requests and events | 11 | Yes, by the rules | Decision, maybe a template gap |
| G8 | The pack definition is in an unknown folder | 2 | Yes | Ours: move to `Definition/` |

No fix below is compiled or checked with an engine run.

<details>
<summary>G1 · A Touch object slice sends the requests of other slices (12)</summary>

**Findings**

1. `Features/Touch/Slices/DragObject/DragObjectHandler.cs(13,9)` · **UMP020101**  
   DragObjectHandler in Handlers.Requests (App/Features/Touch/Slices/DragObject) uses DragResult in Contracts.Requests (App/Features/Touch/Slices/DragScreen): no rule allows it (DefaultPermission = Deny).
2. `Features/Touch/Slices/DragObject/DragObjectHandler.cs(13,9)` · **UMP020101**  
   DragObjectHandler in Handlers.Requests (App/Features/Touch/Slices/DragObject) uses DragScreen in Contracts.Requests (App/Features/Touch/Slices/DragScreen): no rule allows it (DefaultPermission = Deny).
3. `Features/Touch/Slices/DragObject/DragObjectHandler.cs(13,9)` · **UMP020101**  
   DragObjectHandler in Handlers.Requests (App/Features/Touch/Slices/DragObject) uses FindObject in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
4. `Features/Touch/Slices/DragObject/DragObjectHandler.cs(13,9)` · **UMP020101**  
   DragObjectHandler in Handlers.Requests (App/Features/Touch/Slices/DragObject) uses FindResult in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
5. `Features/Touch/Slices/HoldObject/HoldObjectHandler.cs(13,9)` · **UMP020101**  
   HoldObjectHandler in Handlers.Requests (App/Features/Touch/Slices/HoldObject) uses FindObject in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
6. `Features/Touch/Slices/HoldObject/HoldObjectHandler.cs(13,9)` · **UMP020101**  
   HoldObjectHandler in Handlers.Requests (App/Features/Touch/Slices/HoldObject) uses FindResult in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
7. `Features/Touch/Slices/HoldObject/HoldObjectHandler.cs(13,9)` · **UMP020101**  
   HoldObjectHandler in Handlers.Requests (App/Features/Touch/Slices/HoldObject) uses HoldResult in Contracts.Requests (App/Features/Touch/Slices/HoldScreen): no rule allows it (DefaultPermission = Deny).
8. `Features/Touch/Slices/HoldObject/HoldObjectHandler.cs(13,9)` · **UMP020101**  
   HoldObjectHandler in Handlers.Requests (App/Features/Touch/Slices/HoldObject) uses HoldScreen in Contracts.Requests (App/Features/Touch/Slices/HoldScreen): no rule allows it (DefaultPermission = Deny).
9. `Features/Touch/Slices/TouchObject/TouchObjectHandler.cs(13,9)` · **UMP020101**  
   TouchObjectHandler in Handlers.Requests (App/Features/Touch/Slices/TouchObject) uses FindObject in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
10. `Features/Touch/Slices/TouchObject/TouchObjectHandler.cs(13,9)` · **UMP020101**  
   TouchObjectHandler in Handlers.Requests (App/Features/Touch/Slices/TouchObject) uses FindResult in Contracts.Requests (App/Features/Objects/Slices/FindObject): no rule allows it (DefaultPermission = Deny).
11. `Features/Touch/Slices/TouchObject/TouchObjectHandler.cs(13,9)` · **UMP020101**  
   TouchObjectHandler in Handlers.Requests (App/Features/Touch/Slices/TouchObject) uses TouchResult in Contracts.Requests (App/Features/Touch/Slices/TouchScreen): no rule allows it (DefaultPermission = Deny).
12. `Features/Touch/Slices/TouchObject/TouchObjectHandler.cs(13,9)` · **UMP020101**  
   TouchObjectHandler in Handlers.Requests (App/Features/Touch/Slices/TouchObject) uses TouchScreen in Contracts.Requests (App/Features/Touch/Slices/TouchScreen): no rule allows it (DefaultPermission = Deny).

**Rule.** A request handler takes the request of its own slice only (`Handlers.Requests takes Contracts.Requests InSame<Slice>`). Another feature is visible only through its `Domain.Shared` and its open `Contracts.Commands` and `Contracts.Events`. The handlers of `TouchObject`, `HoldObject` and `DragObject` send `FindObject` (feature Objects) and `TouchScreen`, `HoldScreen` or `DragScreen` (other slices of Touch).

**Engine correct?** Yes. In VSA, a slice does not call another slice with a request.

**Proposed fix.** The handler uses the domain directly. `TouchSystem` is in the Domain of its own feature, so the handler may use it. The object search moves from `Objects/Domain/ObjectSystem` to a new `Objects/Domain/Shared/ObjectFinder`, because another feature may use `Domain.Shared`.

Before:
```csharp
internal sealed class TouchObjectHandler : RequestHandler<TouchObject, TouchObjectResult>
{
    readonly IRequestSender requests;

    public override async Task<Result<TouchObjectResult>> HandleAsync(IEnvelope<TouchObject> envelope)
    {
        var found = await requests.SendAsync(new FindObject(envelope.Message.Path), envelope.Cancellation);
        if (!found.IsSuccess) return found.Error;
        var touched = await requests.SendAsync(new TouchScreen(found.Value.Point), envelope.Cancellation);
        if (!touched.IsSuccess) return touched.Error;
        var value = found.Value;
        return new TouchObjectResult(value.Path, value.Name, value.Point, touched.Value.Hit);
    }
}
```

After:
```csharp
internal sealed class TouchObjectHandler : RequestHandler<TouchObject, TouchObjectResult>
{
    readonly ObjectFinder objects;
    readonly TouchSystem touch;

    public override async Task<Result<TouchObjectResult>> HandleAsync(IEnvelope<TouchObject> envelope)
    {
        var target = objects.Find(envelope.Message.Path);
        var point = objects.ScreenPoint(target);
        var hit = await touch.Touch(point, envelope.Cancellation);
        return new TouchObjectResult(envelope.Message.Path, target.name, point, hit != null ? hit.name : null);
    }
}
```

Effect on the trace: today each object command writes three trace nodes (`TouchObject`, `FindObject`, `TouchScreen`). After the fix, it writes one. If we want the inner steps in the trace, this is a decision.

</details>

<details>
<summary>G2 · Two slices share one result type (8)</summary>

**Findings**

1. `Features/Scenes/Slices/LoadScene/SceneResult.cs(5,26)` · **UMP020301**  
   SceneResult in Contracts.Requests (App/Features/Scenes/Slices/LoadScene) has 2 Handlers.Requests types that take it (LoadSceneHandler, ResetSceneHandler): the rule "Contracts.Requests Under<Slice> has Handlers.Requests(Self) exactly 1" needs exactly 1.
2. `Features/Scenes/Slices/ResetScene/ResetScene.cs(5,26)` · **UMP020101**  
   ResetScene in Contracts.Requests (App/Features/Scenes/Slices/ResetScene) uses SceneResult in Contracts.Requests (App/Features/Scenes/Slices/LoadScene): no rule allows it (DefaultPermission = Deny).
3. `Features/Scenes/Slices/ResetScene/ResetSceneHandler.cs(12,9)` · **UMP020101**  
   ResetSceneHandler in Handlers.Requests (App/Features/Scenes/Slices/ResetScene) uses SceneResult in Contracts.Requests (App/Features/Scenes/Slices/LoadScene): no rule allows it (DefaultPermission = Deny).
4. `Features/Scenes/Slices/ResetScene/ResetSceneHandler.cs(12,9)` · **UMP020302**  
   ResetSceneHandler in Handlers.Requests (App/Features/Scenes/Slices/ResetScene) takes SceneResult in Contracts.Requests (App/Features/Scenes/Slices/LoadScene): the rule "Handlers.Requests Under<Slice> takes Contracts.Requests in same Slice" needs both in one Slice.
5. `Features/Wait/Slices/WaitFrames/WaitResult.cs(5,26)` · **UMP020301**  
   WaitResult in Contracts.Requests (App/Features/Wait/Slices/WaitFrames) has 2 Handlers.Requests types that take it (WaitFramesHandler, WaitSecondsHandler): the rule "Contracts.Requests Under<Slice> has Handlers.Requests(Self) exactly 1" needs exactly 1.
6. `Features/Wait/Slices/WaitSeconds/WaitSeconds.cs(5,26)` · **UMP020101**  
   WaitSeconds in Contracts.Requests (App/Features/Wait/Slices/WaitSeconds) uses WaitResult in Contracts.Requests (App/Features/Wait/Slices/WaitFrames): no rule allows it (DefaultPermission = Deny).
7. `Features/Wait/Slices/WaitSeconds/WaitSecondsHandler.cs(12,13)` · **UMP020101**  
   WaitSecondsHandler in Handlers.Requests (App/Features/Wait/Slices/WaitSeconds) uses WaitResult in Contracts.Requests (App/Features/Wait/Slices/WaitFrames): no rule allows it (DefaultPermission = Deny).
8. `Features/Wait/Slices/WaitSeconds/WaitSecondsHandler.cs(12,13)` · **UMP020302**  
   WaitSecondsHandler in Handlers.Requests (App/Features/Wait/Slices/WaitSeconds) takes WaitResult in Contracts.Requests (App/Features/Wait/Slices/WaitFrames): the rule "Handlers.Requests Under<Slice> takes Contracts.Requests in same Slice" needs both in one Slice.

**Rule.** A request contract in a slice has exactly one request handler. A handler takes a contract of its own slice only. `SceneResult` is in the `LoadScene` slice, and the `ResetScene` slice uses it too. `WaitResult` is in the `WaitFrames` slice, and the `WaitSeconds` slice uses it too.

**Engine correct?** Yes.

**Proposed fix.** Move each shared result to the shared folder of the slices of its feature, `Slices/Shared` (part `Slice.Shared`). Contracts and handlers of each slice may use `Slice.Shared`.

```
Before                                         After
Features/Scenes/Slices/LoadScene/SceneResult.cs   Features/Scenes/Slices/Shared/SceneResult.cs
Features/Wait/Slices/WaitFrames/WaitResult.cs     Features/Wait/Slices/Shared/WaitResult.cs
```

The code does not change. Not checked: whether an `IResponse` type in `Slice.Shared` gives another finding.

</details>

<details>
<summary>G3 · Domain and a handler use the module settings class (4)</summary>

**Findings**

1. `Features/Recording/Domain/IVideoRecorder.cs(1,1)` · **UMP020101**  
   IVideoRecorder in Domain (App/Features/Recording/Domain) uses RecordingSettings in Feature (App/Features/Recording): no rule allows it (DefaultPermission = Deny).
2. `Features/Recording/Domain/NoVideoRecorder.cs(8,31)` · **UMP020101**  
   NoVideoRecorder in Domain (App/Features/Recording/Domain) uses RecordingSettings in Feature (App/Features/Recording): no rule allows it (DefaultPermission = Deny).
3. `Features/Recording/Domain/RecordingSystem.cs(14,9)` · **UMP020101**  
   RecordingSystem in Domain (App/Features/Recording/Domain) uses RecordingSettings in Feature (App/Features/Recording): no rule allows it (DefaultPermission = Deny).
4. `Features/Recording/Slices/RecordSession/StartOnSessionStartHandler.cs(14,9)` · **UMP020101**  
   StartOnSessionStartHandler in Handlers.Events (App/Features/Recording/Slices/RecordSession) uses RecordingSettings in Feature (App/Features/Recording): no rule allows it (DefaultPermission = Deny).

**Rule.** `RecordingSettings` is the settings class of `RecordingModule : Module<RecordingSettings>`. It is at the top of the feature folder, so its part is the feature itself. No rule lets Domain or a handler use that part. The same gap gives one G7 finding: `SessionBus` uses `SessionSettings`.

**Engine correct?** Yes, by the rules. But `Configure` registers the settings in the container, so the design expects other classes to get them. The template has no app sample of this. This can be a template gap.

**Proposed fix (ours).** `Configure` copies the values into an options record in `Domain/Shared`. Domain and handlers use the options record.

Before:
```csharp
protected override void Configure(IServiceCollection services, RecordingSettings settings)
{
    services.AddSingleton(settings);
    ...
}

public RecordingSystem(IVideoRecorder recorder, RecordingSettings settings)
```

After:
```csharp
protected override void Configure(IServiceCollection services, RecordingSettings settings)
{
    services.AddSingleton(new RecordingOptions(settings.RecordSessions, settings.FrameRate, settings.Audio, settings.MaxSeconds));
    ...
}

// Features/Recording/Domain/Shared/RecordingOptions.cs
public sealed record RecordingOptions(bool RecordSessions, float FrameRate, bool Audio, int MaxSeconds);

public RecordingSystem(IVideoRecorder recorder, RecordingOptions options)
```

**Question for the template:** how does a part of a module read the module settings?

</details>

<details>
<summary>G4 · The host calls the Session plugin (1)</summary>

**Findings**

1. `App/Host/SpikeShell.cs(9,45)` · **UMP020101**  
   SpikeShell in Host (App/Host) uses SessionBus in Infrastructure (App/Plugins/Session/Infrastructure): no rule allows it (DefaultPermission = Deny).

**Rule.** The host may use Composition, Core, the Umpire libraries, the pack, the definition and the module classes. It may not use the Infrastructure of a plugin.

```csharp
public sealed class SpikeShell : Shell<TestSuite<SpikeHost>>
{
    protected override void OnStop() => SessionBus.AppStopping();
}
```

**Engine correct?** Yes.

**Proposed fix: a decision.** The Session plugin must close the session when the app stops, without the shell. Options:
- A stop hook in the plugin, if Umpire has one (not checked).
- A stop event that the host publishes and the plugin handles.

**Question for the template:** how does a plugin react to the app stop?

</details>

<details>
<summary>G5 · A feature domain uses the Session plugin contracts (1)</summary>

**Findings**

1. `Features/TestSuite/Domain/ScriptCatalog.cs(13,36)` · **UMP020101**  
   ScriptCatalog in Domain (App/Features/TestSuite/Domain) uses TestScript in Contracts (App/Plugins/Session/Contracts): no rule allows it (DefaultPermission = Deny).

**Rule.** Domain may use `Domain.Shared` only. `ScriptCatalog` in `Features/TestSuite/Domain` finds the `TestScript` types (Session plugin contracts) by reflection and makes them.

**Engine correct?** Yes. A class that finds types by reflection is not domain logic.

**Proposed fix.** Move `ScriptCatalog` from `Features/TestSuite/Domain/` to `Features/TestSuite/Infrastructure/`. The code does not change. Not checked: whether Infrastructure of a feature may use the contracts of a plugin.

</details>

<details>
<summary>G6 · ScriptContext in Contracts sends requests (3)</summary>

**Findings**

1. `Plugins/Session/Contracts/ScriptContext.cs(13,9)` · **UMP020101**  
   ScriptContext in Contracts (App/Plugins/Session/Contracts) uses IRequestSender in Contracts.Requests (Umpire.dll): no rule allows it (DefaultPermission = Deny).
2. `Plugins/Session/Contracts/ScriptContext.cs(13,9)` · **UMP020101**  
   ScriptContext in Contracts (App/Plugins/Session/Contracts) uses IResponse in Contracts.Requests (Umpire.dll): no rule allows it (DefaultPermission = Deny).
3. `Plugins/Session/Contracts/ScriptContext.cs(13,9)` · **UMP020101**  
   ScriptContext in Contracts (App/Plugins/Session/Contracts) uses TestRequest`1 in Contracts.Requests (App/Plugins/Session/Contracts/Requests): no rule allows it (DefaultPermission = Deny).

**Rule.** The `Contracts` part holds data types. It may use the message contract types only, not the request API (`IRequestSender`) and not its own `Contracts.Requests`.

**Engine correct?** Yes. `ScriptContext` is not a data type: it sends requests and counts the steps.

```csharp
public sealed class ScriptContext
{
    readonly IRequestSender requests;

    public async Task<TResponse> Send<TResponse>(TestRequest<TResponse> request) where TResponse : IResponse
    ...
}
```

**Proposed fix: a decision.** Move `ScriptContext` and `TestScript` to `Plugins/Session/Presenters/`. In VSA, a presenter is the place where the outside sends requests (`Presenters uses Contracts.Requests`). The lab scripts in `Assets/TestSuiteLab` and `ScriptCatalog` then use a type of the Session presenters. Not checked: whether other modules may use the presenters of a plugin.

</details>

<details>
<summary>G7 · SessionBus and TraceDoneStep in Infrastructure send requests and events (11)</summary>

**Findings**

1. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses IIntegrationEventPublisher in Contracts.Events (Umpire.dll): no rule allows it (DefaultPermission = Deny).
2. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses IRequestSender in Contracts.Requests (Umpire.dll): no rule allows it (DefaultPermission = Deny).
3. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses IRequest`1 in Contracts.Requests (Umpire.dll): no rule allows it (DefaultPermission = Deny).
4. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses IResponse in Contracts.Requests (Umpire.dll): no rule allows it (DefaultPermission = Deny).
5. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses SessionEnded in Contracts.Events (App/Plugins/Session/Contracts/Events): no rule allows it (DefaultPermission = Deny).
6. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses SessionSettings in Plugin (App/Plugins/Session): no rule allows it (DefaultPermission = Deny).
7. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses SessionStarted in Contracts.Events (App/Plugins/Session/Contracts/Events): no rule allows it (DefaultPermission = Deny).
8. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses Shell in UmpireLibraries (Umpire.Unity.dll): no rule allows it (DefaultPermission = Deny).
9. `Plugins/Session/Infrastructure/SessionBus.cs(14,9)` · **UMP020101**  
   SessionBus in Infrastructure (App/Plugins/Session/Infrastructure) uses TestRequest`1 in Contracts.Requests (App/Plugins/Session/Contracts/Requests): no rule allows it (DefaultPermission = Deny).
10. `Plugins/Session/Infrastructure/TraceDoneStep.cs(16,9)` · **UMP020101**  
   TraceDoneStep in Infrastructure (App/Plugins/Session/Infrastructure) uses IIntegrationEventPublisher in Contracts.Events (Umpire.dll): no rule allows it (DefaultPermission = Deny).
11. `Plugins/Session/Infrastructure/TraceDoneStep.cs(16,9)` · **UMP020101**  
   TraceDoneStep in Infrastructure (App/Plugins/Session/Infrastructure) uses StepFailed in Contracts.Events (App/Plugins/Session/Contracts/Events): no rule allows it (DefaultPermission = Deny).

**Rule.** Infrastructure may use Domain, the handler and contract bases, and the message parts. It may not send requests, publish events, use the module settings or use the Umpire libraries (`Shell`).

**Engine correct?** Yes, by the rules. But the two classes have roles that VSA does not show:
- `SessionBus` is the entry from the static CLI commands into the app. It starts the app through `Shell`, sends the request, and publishes `SessionStarted` and `SessionEnded`.
- `TraceDoneStep` is a pipeline step (`AfterStep`). It publishes `StepFailed` when a step fails.

**Proposed fix: a design decision.**

| Class | Option |
|---|---|
| `SessionBus` | Split it. A Session presenter sends the request and publishes the session events. The `Shell` access (start the app, get the services) goes to the host or stays in the CliCommand layer, if our rule allows the Umpire libraries there. |
| `TraceDoneStep` | The step records the failure only. A handler or the presenter publishes `StepFailed`. |

**Questions for the template:**
- Where does the entry from an outside caller (a CLI command, a static API) live?
- May a pipeline step publish an event?

</details>

<details>
<summary>G8 · The pack definition is in an unknown folder (2)</summary>

**Findings**

1. `Packs/TestSuite/Architecture/CliCommand.cs(5,19)` · **UMP020202**  
   CliCommand is in a folder that the definition does not have.
2. `Packs/TestSuite/Architecture/TestSuiteArchitecture.cs(11,13)` · **UMP020202**  
   TestSuiteArchitecture is in a folder that the definition does not have.

**Rule.** Each type must be in a folder of the definition. `Packs/TestSuite/Architecture/` is not one. The pack class `Packs/TestSuite/TestSuite.cs` gives no finding.

**Engine correct?** Yes.

**Proposed fix.** Move `TestSuiteArchitecture.cs` and `CliCommand.cs` to a `Definition/` folder at the block root, as the dice sample does (`Assets/DiceGame/Definition/`). The `Definition` root comes from the base `Architecture.Configure`. Issue #83 moves the other folders.

```
Before                                              After
Spike/Packs/TestSuite/Architecture/TestSuiteArchitecture.cs   Spike/Definition/TestSuiteArchitecture.cs
Spike/Packs/TestSuite/Architecture/CliCommand.cs              Spike/Definition/CliCommand.cs
```

</details>

## Open questions

1. G1: do we want the inner steps (`FindObject`, `TouchScreen`) in the trace of an object command?
2. G4, G6 and G7: the place of the CLI entry, the script API and the app stop hook. These three decide the shape of the Session plugin.
3. For the template: the settings of a module (G3), the app stop in a plugin (G4), an outside caller and an event from a pipeline step (G7).
