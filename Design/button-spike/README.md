# Button spike, phase 1

One button in an HTML design system, one screen, and the same screen made by hand in Unity uGUI. The result is a reference point for later phases: the importer, or a Unity copy of the design system.

## Files

| File | Role |
|---|---|
| `tokens.css` | Tokens in 3 layers: primitive (`--prim-*`), semantic (`--sem-*`), component (`--button-*`) |
| `button.css` | The `ds-button` component and its states: normal, hover, pressed, focus, disabled |
| `gallery.html` | The states, a live button, controls for some primitives, and the token table with references |
| `screen.html` | The 1920 x 1080 screen: background and a button in the bottom-right corner |
| `fonts/` | Inter SemiBold (OFL 1.1), copied from the Unity Editor install |
| `exports/` | Background, 5 button sprites (320 x 108, no label), screen captures |
| `unity/BuildSpike.cs` | One-time builder of the Unity screen. Each number comes by hand from `tokens.css` |
| `unity/GameViewSize.cs` | Sets the Game view to a fixed 1920 x 1080 size for the capture |
| `unity/RecordTouch.cs` | Records a touch that sends uGUI events directly to the button (no hit test) |
| `unity/RecordPointer.cs` | Records a touch through the real input path: `ScriptedInput` -> `StandaloneInputModule` -> raycast |
| `unity/EncodeVideo.cs` | Encodes the frames to MP4 with the built-in `MediaEncoder`. The name comes from `Temp/encode-name.txt` |
| `record-pointer-cdp.mjs` | Records the same touch on `screen.html` with real browser input (headless Edge, CDP) |
| `compare.ps1` | Measures the button and label boxes and the color difference of the two captures |

Unity side: `Assets/UISpike/` (scene `UISpike.unity`, art, font asset, `Scripts/ScriptedInput.cs`). TMP Essential Resources are in `Assets/TextMesh Pro/`.

## How to run

1. Serve this folder: the `button-spike` entry in `.claude/launch.json` (port 8766).
2. Export with headless Edge, for example:
   `msedge --headless=new --hide-scrollbars --force-device-scale-factor=1 --default-background-color=00000000 --window-size=320,108 --screenshot=<out>.png "http://localhost:8766/screen.html?export=button&state=hover"`
   Modes: `export=bg` (1920 x 1080), `export=screen`, `export=button&state=<state>`.
3. In Unity: `unity command script run --file Design/button-spike/unity/BuildSpike.cs --entry BuildSpike.<ImportTmp|PrepareAssets|BuildScene>`.
4. Set the Game view to 1920 x 1080 (`GameViewSize.Set1080p`), enter Play Mode, and capture the Game view.
5. Run `compare.ps1`.

## Unit conversion (by hand)

- The canvas reference size is 1920 x 1080, with Scale With Screen Size. So 1 CSS px = 1 canvas unit.
- Button: anchor and pivot (1, 0), position (-64, 64), size 320 x 108 (face 96 + lip 12).
- 9-slice border: left 22, right 22, top 22, bottom 34 (radius + lip).
- Label: the face area (bottom offset 12). TMP character spacing = 2 px / 40 px x 100 = 5.
- CSS `text-transform: uppercase` = TMP `FontStyles.UpperCase`.

## Result (2026-11-23)

| Measure | HTML | Unity |
|---|---|---|
| Button box | x 1536-1855, y 908-1015 (320 x 108) | same |
| Label box | 105 x 29, y 942-970 | 101 x 29, y 942-970 |
| Mean color difference, button area | 2.33 of 255 | |
| Mean color difference, background | 0 | |

Focus and Disabled: the Unity sprite swap shows the correct sprite.

Evidence in `exports/`: `compare-states.png` (5 states, HTML and Unity), `unity-touch.mp4` (direct events), `unity-pointer.mp4` and `pointer-steps.png` (real input path), `button-gallery.html` (self-contained gallery).

## Input in Unity

| Method | Path | Result |
|---|---|---|
| `runtime simulate_pointer` (pipeline) | Input System devices | Refused, exit 6: needs `com.unity.inputsystem`. No legacy input path. |
| `ExecuteEvents` to the button (`RecordTouch.cs`) | Straight to the button, no hit test | Works, but proves only the button code |
| `ScriptedInput` as `inputOverride` (`RecordPointer.cs`) | `StandaloneInputModule`, raycast, events | Works: a tap 36 px outside the button gives 0 clicks, a tap on it gives 1 |

`ScriptedInput` counts a press or release in the first frame that the module reads it. The first version used the frame of the `Press()` call, and the module never saw the press.

## Input in HTML

`record-pointer-cdp.mjs` does the same steps on `screen.html`, at the same points. It starts headless Edge and sends `Input.dispatchMouseEvent` over the Chrome DevTools Protocol. The browser does the hit test, `:hover`, `:active` and `click`. Node 22 or later has `WebSocket`, so no package is necessary.

| Run | Frames | Clicks after the tap outside | Clicks at the end |
|---|---|---|---|
| HTML, CDP (`html-pointer.mp4`) | 148 | 0 | 1 |
| Unity, `ScriptedInput` (`unity-pointer.mp4`) | 148 | 0 | 1 |

After the click, HTML shows the hover state. Unity shows the focus ring (see "Gaps found").

## Gaps found

| Gap | Cause | Possible fix |
|---|---|---|
| The label is 4 px narrower in Unity | CSS adds letter spacing after the last letter too. TMP does not. | Accept, or remove 1 spacing from the CSS width |
| The disabled label stays dark in Unity | Sprite Swap changes the image only. The HTML label turns gray. | A second graphic transition for the label color |
| The pressed label does not move down in Unity | In HTML the label moves with the face (8 px). Sprite Swap does not move children. | A small state component, or an animation transition |
| HTML was 4 px too high at first | An inline-block button in a div adds the line descent under it | Fixed: `line-height: 0` on `.corner` |
| After a click, uGUI shows the focus ring | `Selectable` selects the button on pointer down. HTML shows the ring only for keyboard focus (`:focus-visible`). | Do not select on pointer, or map Selected to hover |
