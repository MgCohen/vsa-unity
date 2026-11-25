# Unity builder: hand-over

Roadmap item A8. Goal: one JSON entry builds a simple native button in Unity. The plan and the JSON exist. No Unity code exists.

## Done

| Item | Path or link | State |
|---|---|---|
| Plan artifact | https://claude.ai/artifact/3dB6ck5M1T6YWT4mWV4Fkk | Version 1, private |
| Plan source | `Design/button-spike/builder-plan.html` | Same content as version 1 |
| Button JSON | `Design/button-spike/components.json` | Parses; 12 of 22 `--button-*` tokens |

Not verified: no browser check of the plan page (375 px, dark theme). No code sketch in the plan was compiled.

## User decisions

None. The user has not answered the plan. Do not start the spike before the user agrees.

## Unfinished requests

- The user's agreement to the plan.
- The spike: base component, built button, 5 state captures, touch video, one error or warning case.

## Known defects in the plan sketches

| Sketch | Defect | Correction |
|---|---|---|
| `DsSelectable` label position | Resets to zero, but the inset label rests at y = 6 | Store the rest position |
| `DsSelectable` pressed offset | CSS `top: 8px` is down; the sign is not stated | Subtract 8 in uGUI |
| `DsSelectable` focus rule | uGUI ranks Selected above Highlighted, so hover does not return after a click | Examine the pointer state (also corrects Q4) |
| `keyboardSelect` | Nothing sets it | Set it in `OnSelect`, before the base call |

The first three come from a read of the sketch and of uGUI behavior from memory. Test them in the spike.

The plan also has empty table cells. The sample results in the plan are hand-written, not tool output.

## Open questions

1. Build trigger: context menu and manifest field change (recommended), or also manifest import?
2. Manifest location: copy in `Assets/UISpike/` (recommended), or a path outside `Assets/`?
3. General name for computed geometry such as `child-offset-y`. The component-data session can decide.
4. Look after a pointer press: Normal or Highlighted? HTML shows hover.

## Next step

1. Ask the user to agree to the plan and to answer questions 1, 2 and 4.
2. Correct the sketches in `builder-plan.html`. Publish to the same link (pass it as `url`, read it first).
3. Do the spike steps in the Edit flow tab. On a different computer, open the Editor on the project again.

The spike uses: `Assets/UISpike/` (sprites, font, `ScriptedInput.cs`), `Assets/TextMesh Pro/` and `Design/button-spike/unity/`.
