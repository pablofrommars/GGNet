# HANDOVER — GGNet Plot-Composition Skill

> Regrounded 2026-07-06 against the actual repo (branch `ai-refactor-feb2026`, pre-`2.0.0` surface).
> The prior session designed this blind; everything below is now verified against source.
> Planning and phase ordering live in [../PLAN.md](../PLAN.md) — this file carries the skill's design only.

## Goal

Author a reusable skill that gives a frontier model mermaid-level fluency in generating plots with **GGNet** — a grammar-of-graphics (ggplot2-inspired) charting library for .NET / Blazor. Skills are vendor-neutral assets (portable markdown, no Claude-Code-only mechanics in the content); Claude Code's SKILL.md convention is the packaging baseline.

## Ground truth (was wrong or unknown in the original design)

- **Library**: GGNet, NuGet package `GGNet`. The published 1.4.0 docs are stale — the repo is the pre-tag **2.0** surface with breaking renames (`width`→`strokeWidth`, `alpha`→`opacity`, `_color`→`colorBy` family, `format:`/`timezone:`→`formatter:`, `GGNet.Static`→`GGNet.Headless`; full list in `ROADMAP.md`). The skill documents 2.0; anything the model knows from 1.x is itself a common-mistake to guard against.
- **The API is a fluent C# DSL, not component markup.** There is no `<Aes>` component (the prior session's assumption). A plot is one chain: `PlotContext.Build(source, x, y).Geom_*(...).Scale_*(...).Facet_*(...).Style()`. The Blazor components (`Plot`, `SparkLine` under `src/GGNet/Components/`) only *host* a built plot. Examples are therefore **C# plot-construction code**, with at most one thin `.razor` hosting wrapper — not 15–20 `.razor` files.
- **Aesthetics**: `xxxBy` parameters (`colorBy`, `fillBy`, `sizeBy`, `lineTypeBy`) take mappings built by `Scale_Color_Discrete`, `Scale_Fill_Continuous`, …; the unsuffixed twin is a per-layer constant. Positional arguments stop at the selectors; everything after is named. Vocabulary is SVG's.
- **Stats are a first-class concept the original design missed entirely**: `Stat.Bin` / `Stat.Density` / `Stat.Count` / `Stat.Summary` are typed *sources*, not layers — "a histogram is `Stat.Bin` + `Geom_Bar`; there is no Histogram geom". Per-facet stats = grouped stats (`groupBy:` + facet on the same key).
- **Theming is CSS-variable based**, not a ggplot theme-object API: layout is C# (`Style`), paint is CSS via `--ggnet-*` variables scoped under `.ggnet[theme=name]`. Reference file must teach that split, not `theme_minimal()`-style calls.
- **Faceting** is two calls (`Facet_Wrap`, `Facet_Grid` with `freeX`/`freeY`) — thin; folds into SKILL.md or the scales reference rather than its own file.
- **Validation can be stronger than compile-check**: `src/GGNet.Headless` renders to SVG server-side, and `tests/GGNet.Headless.Tests/Gallery/` byte-pins gallery snapshots. The validator compiles **and renders**.

## Agreed design (updated, do not relitigate)

**Decision (2026-07-06): examples are compiled gallery entries.** Each example lives as a real entry in `tests/GGNet.Headless.Tests` — compiled under `-warnaserror`, rendered via Headless, byte-pinned. The skill's `examples/` files are extracted from those entries, so CI makes silent drift impossible.

**Skill layout**

```
skills/ggnet/                 # vendor-neutral home at repo root, tracked in git
├── SKILL.md                  # index; < 500 lines; loads in full on trigger
├── reference/
│   ├── geoms.md              # the 21 geoms: selectors, mappings, constants, events
│   ├── stats.md              # Stat.* sources + which geom draws each
│   ├── scales.md             # position/aesthetic scales, formatters, facets
│   ├── theming.md            # Style (layout) vs CSS variables (paint); export
│   └── chart-selection.md    # distilled from ../decision-tree/chart_selection.json
├── examples/                 # 15–20 extracted from compiled gallery entries
├── patterns/
│   └── common-mistakes.md    # R-ggplot2 priors + GGNet 1.x priors + DSL conventions
└── scripts/                  # compile + headless-render validator
```

**Principles** (unchanged where still valid)

1. Progressive disclosure — SKILL.md is an index; detail lives in reference files loaded on demand.
2. SKILL.md leads with the compositional mental model — now stated in repo terms: `Build(source, selectors) + Geom_* layers + Stat.* sources + Scale_* + Facet_* + Style/theme` — plus a minimal working example and the 4-step decision flow (data shape → chart/geoms → mappings → facets/scales/theme).
3. Examples carry the most weight — cover every major geom, stat+geom combinations, faceting, custom scales, theming, interactivity (tooltip/events block).
4. Negative space: `common-mistakes.md` stops both R syntax (`aes()`, `+` chaining, `geom_histogram`) and GGNet 1.x parameter names leaking from priors.
5. Frontmatter description is a classifier: name GGNet, list trigger phrases and C#/Blazor context, anti-trigger Plotly/ChartJS/ApexCharts/ScottPlot.
6. "R ggplot2 → GGNet" translation section (5–10 side-by-side examples).
7. Validation script the agent runs post-generation: compile against the solution **and render to SVG via GGNet.Headless** before showing the user.
8. Chart-type selection is distilled into `reference/chart-selection.md` from the decision-tree config during the skills-only phase; the C# MCP later becomes the deterministic path, same vocabulary by construction. Unsupported leaves name the nearest GGNet alternative (annotations live in the config — see PLAN.md).

## Next steps (in order)

1. Inventory the repo API: geom signatures (21 `BuilderExtensions.*.cs` partials), `Scale_*` family, `Stat.*` sources, facets, `Style` surface, theming variables, Headless export.
2. Define the 15–20 example set (chart-type coverage matrix, keyed to chart-selection leaves); add any missing entries to the Headless gallery tests.
3. Build the extraction step: gallery entry → `examples/` file.
4. Draft SKILL.md frontmatter; verify triggers against real user phrasings.
5. Write SKILL.md body (mental model, minimal example, decision flow, quick component table, pointers).
6. Write `reference/*.md` from actual API (not memory); generate `chart-selection.md` from `chart_selection.json`.
7. Write `patterns/common-mistakes.md` — seed from real errors when generating without the skill, plus the 1.x→2.0 rename list.
8. Build `scripts/` validator (compile + headless render).
9. Evaluate: trigger accuracy + generation quality; iterate.

## MCP extension (ships after the skill; designed together — see PLAN.md)

- **C# all the way** (decision 2026-07-06): the Python `engine.py`/`mcp_server.py` under `../decision-tree/` are the prototype; production is a .NET MCP server (official `ModelContextProtocol` C# SDK). `chart_selection.json` is reused as-is (plus GGNet capability annotations); the 20-case eval ports to xUnit.
- **Tools**: `select_chart` / `list_charts` / `explain_axes` (ported), plus composition-side `validate_plot` (wraps the skill's compile+render validator) and live API introspection (`list_geoms` / `list_scales`).
- **Skill vs MCP**: skill = knowledge (portable, works in any agent surface); MCP = executable guarantees (deterministic selection, validated output, never-stale API surface). Shared vocabulary — selector leaf ids ↔ geom recipes — is the design-together contract.

## Resolved (formerly open) questions

- ~~Library name / NuGet ID~~ → GGNet / `GGNet`.
- ~~`<Aes>` component or attribute mappings?~~ → neither; `xxxBy` lambda parameters + `Scale_*` calls on the fluent chain.
- ~~Install location~~ → `skills/ggnet/` at repo root: skills target frontier models across vendors; repo-tracked, shipped with the library.

## Still open

- Gallery-entry → example-file extraction mechanics (build step vs checked-in copies with a consistency test).

*Resolved: target agents are Claude and Codex (priority order); content formulated for both, packaging may be vendor-specific — see PLAN.md D8.*
