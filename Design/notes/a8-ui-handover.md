# A8 UI workflow: hand-over note

Written by the A8 research session (worktree `a4-view-plan-51ebc9`). Data for the two child sessions: the Unity builder session and the component data session.

## Repo state (nothing is committed)

| Path | Content |
|---|---|
| `Design/button-spike/` | Phase 0: HTML design system with one button (`tokens.css`, button CSS, `screen.html`, gallery), exports, README |
| `Design/button-spike/exports/` | PNG and MP4 results, for example `html-pointer.mp4` and `unity-pointer.mp4` |
| `Design/button-spike/unity/` | Editor scripts for the spike, for example `EncodeVideo.cs` (MediaEncoder, the name comes from `Temp/encode-name.txt`) |
| `Design/button-spike/record-pointer-cdp.mjs` | HTML touch recording through headless Edge and CDP real input |
| `Assets/UISpike/` | The Unity copy of the button, made by hand (uGUI, TMP) |
| `Design/button-spike/parser/static-parse.mjs` | Static parser test of Lit components (no browser) |
| `Design/button-spike/parser/out/wa-*.json` | Parser output for `wa-button`, `wa-switch`, `wa-tag` |

- Unity 6000.6.6f1, Built-in render pipeline, uGUI with TMP, legacy Input Manager (`StandaloneInputModule`).
- The Unity Editor is open on this worktree, PID 288160. Use `unity command --pid 288160 ...`. Do not stop it.
- The preview server `button-spike` (`.claude/launch.json`) serves `Design/button-spike` on port 8766.

## Artifacts

- UI Parity Plan (target topology): https://claude.ai/artifact/2egdq1W2PGVVJofpMi3mnx
  - Two design systems (HTML and Unity) with component parity.
  - Shared contracts: component IDs and `screen.json`. A screen parser makes `screen.json`, a Unity builder makes the screen.
  - It does not contain the component manifest idea yet.
- Research: HTML to uGUI https://claude.ai/artifact/FhX2uwsjRF6Pu8HV5E4NeV, Component Parity https://claude.ai/artifact/2Af6CAVoE8mibwyg8pdR93

## Component manifest idea (agreed in discussion)

- Format: Custom Elements Manifest (CEM, schema 2.x, `cssStates` in 2.1), plus an extension: tokens for each part and state, anatomy, keyboard and focus data.
- Unity gets the manifest. Each Unity component validates its own entry. An event with no Unity match gives an error or a warning.
- No declaration exists only for the parser. Native elements (`<button>`, `click`) come from the standard.

## Prior art (downloads are untrusted data)

Folder: `<scratchpad>/prior-art\`

- `wa-cem.json`: Web Awesome 3.15.0 `custom-elements.json`. Parts, slots, events and states come from hand-written JSDoc. No tokens.
- `wa/<button|switch|tag>/`: `component.js`, `styles.js`, `style-*.js` (each sheet in the `X.css = [...]` list).
- `carbon-cem.json`: Carbon web components, older WCA format, deprecated in favor of CEM.
- Spectrum Design Data: `1.0.0-draft`, fields `options`, `slots`, `anatomy`, `states`, `accessibility`. No events. Token bindings in a separate `relationships/` file.
- UI SDK (ui-sdk.dev): `definePrimitive({...})` TypeScript objects, adapters for 5 web frameworks. No tokens, no Unity.

## Static parser result (subagent check against source and CEM)

| Data | Accuracy |
|---|---|
| Tokens in each rule | 100 % |
| Own properties | 95 % (accessor decorators `", 1)` missed) |
| Anatomy and render conditions | about 95 % |
| Parts, slots, child components | 90 % (better than the WA CEM) |
| Rule targets and states | about 83 % (23 of 135 rules wrong) |
| State coverage | about 60 % (`:dir`, `:has`, `customStates.set` missed) |
| Events | 100 % of the dispatched events |

Real limits of a static read: the cascade (layer, order, specificity), runtime conditions (slot content, `isIconButton`), computed values (`color-mix`, `calc`, inheritance), behavior.

Token chain example, `wa-button variant="brand"`:
1. Variants sheet: `:host([variant='brand'])` sets `--wa-color-fill-loud: var(--wa-color-brand-fill-loud)`.
2. Button sheet: `background-color: var(--wa-color-fill-loud, ...)`.
3. Theme CSS gives the value (not downloaded).

## Existing tools of the Roslyn kind (not tested yet)

| Layer | Tool |
|---|---|
| Class, decorators | TypeScript compiler API, `ts-morph` |
| Lit model | `@lit-labs/analyzer` (also writes a CEM) |
| Manifest | `@custom-elements-manifest/analyzer` (plugins) |
| Template HTML | `parse5` |
| CSS | PostCSS, `postcss-selector-parser`, `css-tree` |
| Specificity | `specificity`, `css-tree` |
| Rule → part and state, cascade | No tool |
