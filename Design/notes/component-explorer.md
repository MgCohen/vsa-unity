# Component explorer

The component explorer is a page that breaks down two buttons: `wa-button` (Web Awesome 3.15.0) and `ds-button`.

## Done

- Artifact: https://claude.ai/artifact/U6SZnKXrUmGdjcQDzegTMo (private, version 8). `Design/component-explorer/index.html` is the same page.
- The tree shows files, then kinds, then items. A select shows the source lines.
- Hover links the preview and the tree. The path shows the level and the way up.
- With nothing selected, the source panel shows the minimal page of the button.
- The Web Awesome source (tag v3.15.0, MIT) is in `sources/`. The user permitted the download.

## Decisions by the user

- Use the same groups for both buttons. Break down by file first, then by kind.
- Show only what is on the button, one level deep. Click to go deeper.
- Put explanations in tooltips. Do not add style or depth controls. Keep the mobile layout clean.
- Keep the source always visible. Keep the hover highlight in the preview.
- Show only the necessary lines on the first screen.

## Files in `Design/component-explorer/`

| Path | Purpose |
|---|---|
| `build.mjs` | Reads the sources and writes `index.html` |
| `page.html` | Page template: CSS and script |
| `pages/` | The minimal page of each button |
| `sources/` | Web Awesome source and license |
| `tests/` | `t6.mjs` page behavior, `t7.mjs` preview against real pages, `cem.mjs` parser against the manifest |
| `shots/` | Screenshots |

The build also reads `Design/button-spike/`: `tokens.css`, `button.css`, `components.json`, `fonts/Inter-SemiBold.ttf`.

## Build and test

Run these commands in `Design/component-explorer/`. The tests need Node and Microsoft Edge on Windows, in `Program Files (x86)`. `t7` needs internet access.

```
node build.mjs
node tests/t6.mjs index.html
node tests/t7.mjs index.html <absolute path of component-explorer, with forward slashes>
node tests/cem.mjs index.html <custom-elements.json of Web Awesome 3.15.0>
```

- `t6`: 10 of 10 pass.
- `t7`: 5 of 6 pass. `display` of `ds-button` is `block` in the preview and `inline-block` on the real page. The preview grid causes this.
- `cem.mjs`: its manifest is not in the repository.
- If a test stops and waits, close the headless Edge process from the test (port 9377).

## Limits

- The preview is a static copy. It does not run Web Awesome code, because an artifact cannot load the theme CSS.
- The two pages in `pages/` and the preview background of `ds-button` are not from the source projects.
- `ds-button` does not need the label span for its look. It is in the page because `components.json` declares it.

## Unfinished requests

None. The last messages were questions about `<wa-button>` and custom elements. Both have answers.

## Open questions

- None from the user.

## Next step

Wait for the user. If the preview must match the real page in all styles, put the `ds-button` in a wrapper in the preview.
To publish from a new session, read the artifact first, then publish `index.html` with its URL.
