# Component data: hand-over

Goal: find the data that a complex component needs to go from HTML to Unity (roadmap A8), and the tools to get it. Test case: `wa-button`, Web Awesome 3.15.0.

## Done

- Page: Component Manifest Levels, version 1 (`1795477584-7d28`), https://claude.ai/artifact/CnioTjUsVvQDWLdY7GQ9Pr. Private.
- Page source: `Design/button-spike/parser/component-manifest.html`.
- Samples, written by hand: `Design/button-spike/parser/samples/wa-button.manifest.json` and `ds-button.manifest.json`.
- Source of the parser data, from the A8 research session: `parser/static-parse.mjs` and `parser/out/wa-*.json`.

Related page: UI Parity Plan, https://claude.ai/artifact/2egdq1W2PGVVJofpMi3mnx.

## Proposal (the user did not review it)

| Level | Content | Place |
|---|---|---|
| L0 API | Attributes, events, slots, parts, states | Standard CEM |
| L1 Anatomy | Nodes, child components, conditions | `ui.anatomy` |
| L2 Styles | Ordered patches: node + condition → token values | `ui.styles` |
| L3 Tokens | Variant scopes, theme values | `ui.tokenScopes`, `tokens.json` (DTCG) |
| L4 Samples | Computed values of some states | Test fixture |

## Corrections to the page and to the report

The page and the report state these items too strongly. Correct the page before the review.

| Statement | Fact |
|---|---|
| "About 40 patches" | Not counted. The parser gives 74 bindings for `wa-button`. |
| 48,000 combinations | Product of the axes. Some combinations cannot occur. |
| Pairwise in about 40 states, 150 lines of own code | Guesses |
| Edge CDP is exact, key tool | Not tested on a component. The CSS domain calls are from memory. |
| Tool results for each level | No npm tool was tested or read. They are from memory. |
| `ds-button`: 8 patches | Only the sample has 8. It omits the disabled face and the focus shine, and more. |
| `#0071ec`, `oklab(...)`, `40px` | Invented sample values |
| `wa-color-brand-50` alias | Invented. The theme CSS was not read. |
| Patch order holds the cascade | Not proved. `!important`, `::part` rules of the consumer and inheritance are not in the model. |
| `var()` fallbacks | The samples omit them. |

## Decisions by the user

- In this session: none.
- From the brief: CEM plus an extension, each Unity component validates its own entry, and no declaration exists only for the parser.

## Unfinished requests

- The user review of the page (necessary for "done").
- An answer to: install the npm tools in the scratchpad for the L2 test? The user did not answer. Nothing is installed.

## Open questions

1. HTML components: custom elements or CSS classes?
2. Extension: a `ui` field in the CEM declaration, or a separate file? The CEM schema is not examined.
3. Unity: live tokens or baked values for each state?
4. Where does the behavior data come from? The `ui.behavior` sample is a hand-written declaration. It can conflict with the rule against declarations only for the parser.

## Next step

1. Correct the page, then let the user review it.
2. If the user agrees to the install, test L2: PostCSS on the 3 sheets of `wa-button`, then compare with Edge CDP for 10 states.

The Web Awesome downloads are in a temporary folder and are not in the repo. Get them again from the `@awesome.me/webawesome` 3.15.0 package (verify the name). Ask the user before the download.
