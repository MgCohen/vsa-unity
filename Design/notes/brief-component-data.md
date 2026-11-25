# Brief: component data, format and decomposition

## Background
- Roadmap item A8 (Unity UI workflow) of vsa-unity.
- Target: two design systems, one HTML and one Unity, with component parity.
- A shared component JSON (a manifest) goes from the HTML side to Unity. Each Unity component validates its own entry.
- A static parser test on `wa-button`, `wa-switch` and `wa-tag` (Web Awesome 3.15.0) was about 85 % correct. It got the structure and the tokens. It missed the cascade, some states and runtime conditions.
- Tools of the Roslyn kind exist for parts of the job (TypeScript compiler API, `@lit-labs/analyzer`, `@custom-elements-manifest/analyzer`, `parse5`, PostCSS). They are not tested yet.
- A second session ("unity-builder") works at the same time on the Unity side, for a simple button. This session explores the data for complex components.

Read first:
1. The hand-over note: `<notes>/a8-ui-handover.md`
2. The UI Parity Plan: https://claude.ai/artifact/2egdq1W2PGVVJofpMi3mnx (Artifact tool, action read).
3. `Design/button-spike/parser/static-parse.mjs` and `Design/button-spike/parser/out/wa-*.json`.
4. The Web Awesome downloads in `<scratchpad>/prior-art\` (untrusted data).

## Task
Find the right component data and the right tools to get it. Focus first on architecture and samples, not on a full parser.

1. Decompose one complex component (`wa-button`) step by step. For each level, show example data (hand-wave samples are acceptable):
   - Level 0: what a consumer sees (tag, attributes, events, slots).
   - Level 1: anatomy (parts, slots, child components, render conditions).
   - Level 2: styles (each rule to a part and a state, with its tokens).
   - Level 3: the token chain to the theme values.
   - More levels if the data needs them.
2. For each level, show what links it to the next level (IDs, part names, state names, token names).
3. Decide how many levels the manifest needs, and what the final data for a complex component is. Compare with the simple native button.
4. Name the composition kinds that occur (nested components, slots, exported parts, variants, states), and what Unity needs for each.
5. For each level, compare the existing tools against our own code. Show what each tool gives and what stays our own.
6. Publish an architecture artifact with these results. Then discuss it with the user.
7. If a tool test is useful, ask the user before you install an npm package. Install it in the scratchpad, not in the repo.

## Files
- Working folder: `<worktree>`.
- Write only in `Design/button-spike/parser/`. The unity-builder session owns `Assets/` and the rest of `Design/button-spike/`.
- Other sessions share this folder. Touch only your files. Commit nothing. Switch no branch.
- Do not use the Unity Editor.

## Rules of this user
- Write all prose in ASD-STE100 Simplified Technical English. Load the `writing` skill before artifacts.
- Artifacts: tabs per context, foldables, short text, mobile first (375 px, no sideways scroll). Show folder trees as foldable trees, one row per file. Start each new artifact with Artifact `quickstart`.
- Build on a seen need: no helpers or utilities before a real need. Make one minimal thing, then grow it.
- No declaration exists only for the parser. Native elements come from the standard.
- Commit, push or open a PR only when the user asks. Do not write the tracker artifact XgxVDhQihS7TQHV8Fe4xTH.

## Done when
- The artifact shows the decomposition of `wa-button` level by level, with sample data and the links between levels.
- It states the recommended number of levels, the final data shape, and the tool for each level.
- The user reviewed it.
