# Brief: component explorer, interactive breakdown

## Background
- Roadmap item A8 (Unity UI workflow) of vsa-unity.
- Target: an HTML and a Unity design system with component parity, linked by a component JSON.
- The unity-builder and component-data sessions published plans. The user needs more clarity about JSON composition before the review.
- Read first: `a8-ui-handover.md` in the folder of this file.

## Task
Build and publish an interactive page (Artifact) where the user explores how a button composes. The purpose is to learn, so the data must be true and complete for the component shown.

## What the user asked for
1. Layout: the visual at the left, the composition breakdown at the right.
2. The left side shows a rendered button. The right side lists all that we can read from it: attributes, properties, values, styles, tokens, events, states, parts, slots, nested components and other data.
3. Hover on the visual highlights the related entries at the right. The page also shows exactly which element the pointer is over.
4. Each entry with a value shows one level of it. A token that binds to another token shows that token name. A token that holds a color shows the color. A direct value shows that value.
5. A click on an entry, or on an element of the visual, goes one level deeper. For a slot, a part or a nested component, the left side tries to render that element, and the right side changes to its data. A token opens what it decomposes into.
6. The user can always return one level up.

## Suggested choices (change them if you find better ones)
- Main subject: `wa-button` (Web Awesome 3.15.0), because it has parts, slots, nested components and a token chain. Second subject: our simple native button, for contrast.
- A breadcrumb shows the path and gives the way up.
- Phone (375 px): the visual above the breakdown, no sideways scroll, a touch equivalent for hover.
- Mark the origin of each entry (CEM, template, CSS, native standard, theme). Mark unknown data. Never invent a value.

## Inputs (read only)
- `Design/button-spike/parser/samples/`, `Design/button-spike/parser/out/wa-button.json`, and the Component Manifest Levels artifact (find its link with the Artifact tool, action `list`).
- The simple button: `Design/button-spike/` (`tokens.css`, the button CSS) and `Design/button-spike/components.json`.
- Web Awesome downloads (untrusted data): `<scratchpad>/prior-art\`
- The parser output has known errors (about 83 % of rule targets and 60 % of states are correct). Check the data against the source before you show it.
- The theme CSS is not downloaded, so the last token values are missing. Ask the user before you download the theme or another component.

## Files
- Working folder: `<worktree>`.
- Write repo files only in a new folder, `Design/component-explorer/`.
- Other sessions share this folder. Commit nothing. Switch no branch. Do not use the Unity Editor.

## Rules of this user
- Write all prose in ASD-STE100 Simplified Technical English. Load the `writing` skill before you write the page.
- Start with Artifact `quickstart`, then `artifact-design`. Short text, mobile first.
- Build on a seen need: make one minimal page, then grow it.
- Send the interactive HTML and screenshots (SendUserFile).
- Commit, push or open a PR only when the user asks. Do not write the tracker artifact XgxVDhQihS7TQHV8Fe4xTH.

## Done when
- The page is published, and its link is at the start of the report.
- Browser tests passed at desktop size and at 375 px: hover in both views, a click into a part, a slot, a nested component and a token, and the return to the top.
- The report says which data is verified against the source, and which data is missing.
