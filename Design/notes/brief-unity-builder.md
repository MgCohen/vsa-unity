# Brief: Unity builder, one JSON builds a button

## Background
- Roadmap item A8 (Unity UI workflow) of vsa-unity.
- Target: two design systems, one HTML and one Unity, with component parity.
- A shared component JSON (a manifest) goes from the HTML side to Unity. Each Unity component validates its own entry. An event with no Unity match gives an error or a warning.
- A static parser test on 3 Web Awesome components showed that a simple native button with tokens needs no complex tooling.
- A second session ("component-data") works at the same time on the JSON format and the decomposition of complex components. This session does the Unity side, for a simple button only.

Read first:
1. The hand-over note: `<notes>/a8-ui-handover.md`
2. The UI Parity Plan: https://claude.ai/artifact/2egdq1W2PGVVJofpMi3mnx (Artifact tool, action read).

## Task
Plan, then spike, a Unity builder: one JSON goes in, one base component builds a matching button.

1. Read the note, the plan, `Design/button-spike/` (the HTML button and its tokens) and `Assets/UISpike/` (the button made by hand).
2. Make one JSON for the simple native button with its tokens. Use the simplest format (CEM plus a small extension is acceptable). Write it by hand or extract it. Do not build a general parser.
3. Brainstorm what "matching" means: component ID, properties, events, states, tokens, sprites. Define what gives an error and what gives a warning.
4. Design the architecture. One base MonoBehaviour receives the JSON and edits itself. It adds what the entry needs (Image, Button, TMP label, sprites, state colors). It is modular: a label entry or an image entry adds only its own parts. It is not a generic component system yet.
5. Publish an architecture artifact. Show step by step with samples: the JSON in, the components that the builder adds, the validation result, the edit flow.
6. Stop and discuss the artifact with the user. Start the Unity spike only after the user agrees.
7. Spike: one JSON, one base component, a built button that matches the HTML button. Send screenshots and a touch video.

## Files
- Working folder: `<worktree>`.
- Write only in `Assets/` and `Design/button-spike/`, but not in `Design/button-spike/parser/`. The component-data session owns that folder.
- Other sessions share this folder. Touch only your files. Commit nothing. Switch no branch.

## Rules of this user
- Write all prose in ASD-STE100 Simplified Technical English. Load the `writing` skill before artifacts.
- Artifacts: tabs per context, foldables, short text, mobile first (375 px, no sideways scroll). Show folder trees as foldable trees, one row per file. Start each new artifact with Artifact `quickstart`.
- Build on a seen need: no helpers, recorders or loggers before a real need. Make one minimal thing, then grow it.
- Zero install: the Unity side works with only the Unity Editor.
- The Unity Editor is open on this folder, PID 288160. Use `unity command --pid 288160 ...` and recompile between runs. Do not stop it. The user says when the Unity work is done.
- Send screenshots, interactive HTML and a Unity touch video for each UI result (SendUserFile).
- Tests: break code in the test project itself to prove that a test fails.
- Commit, push or open a PR only when the user asks. Do not write the tracker artifact XgxVDhQihS7TQHV8Fe4xTH.

## Done when
- The architecture artifact is published and the user agreed to it.
- After the spike: a Unity button built from the JSON, with screenshots and a touch video, and a validation output that shows one error or warning case.
