# UI workflow notes (roadmap item A8)

Start here to continue the work on another computer. The branch is `wip/a4-view-plan-51ebc9`.

## Goal

- Two design systems, one HTML and one Unity, with component parity.
- HTML gives a component JSON and a token JSON. Each Unity component validates its own JSON entry. An event with no Unity match gives an error or a warning.
- A screen parser makes `screen.json` with the tree, the component IDs, the sizes and the anchors. Unity builds screens from it.

## Streams

| Stream | Note | Page |
|---|---|---|
| Unity builder | [unity-builder.md](unity-builder.md) | https://claude.ai/artifact/3dB6ck5M1T6YWT4mWV4Fkk |
| Component data | [component-data.md](component-data.md) | Component Manifest Levels (link in the note) |
| Component explorer | [component-explorer.md](component-explorer.md) | https://claude.ai/artifact/U6SZnKXrUmGdjcQDzegTMo |
| Target topology | [a8-ui-handover.md](a8-ui-handover.md) | https://claude.ai/artifact/2egdq1W2PGVVJofpMi3mnx |

## Decisions

- Use two design systems with component parity. Parse screens as components.
- Use the Custom Elements Manifest format, with an extension for tokens, anatomy, keyboard and focus.
- A declaration never exists only for the parser. Native data comes from the standard.
- Read definitions statically. Use a browser only for final values.
- Explore the simple button and the complex components in separate streams.
- Keep zero install on the Unity side. Build a helper only after a real need.

## Open questions

- Phase 1 questions Q1 to Q5 in the UI Parity Plan.
- Addition of the component manifest to the UI Parity Plan.
- Tools for each manifest level. The analyzers in the hand-over note are not tested.

## Restore

1. Clone the repository and check out `wip/a4-view-plan-51ebc9`.
2. Open the project with Unity 6000.6.6f1.
3. Start the `button-spike` server of `.claude/launch.json` to see the HTML pages.

- `plan-pages/` holds the sources of the published plan and research pages.
- The `brief-*.md` files are the first tasks of the three streams. Their local paths, Editor PID and session rules are from the old computer.
- The Web Awesome downloads are version 3.15.0 from npm. `Design/component-explorer/sources/` holds a part of the source with its license.
